using System.Collections;
using System.Reflection;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Batch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.Json;
using Microsoft.OData.ModelBuilder;
using ServiceStack;
using Undersoft.SDK.Service.Data.Client.Attributes;
using Undersoft.SDK.Service.Data.Model;

namespace Undersoft.SDK.Service.Server.Builders;

public class DataServiceBuilder<TStore> : DataServerBuilder, IDataServerBuilder<TStore>
    where TStore : IDataStore
{
    protected IEdmModel edmModel;
    protected ODataConventionModelBuilder odataBuilder; 
    protected static bool actionSetAdded;

    public DataServiceBuilder(IServiceRegistry registry)
        : base(null, typeof(TStore), registry)
    {
        odataBuilder = new ODataConventionModelBuilder();
    }

    public DataServiceBuilder(IServiceRegistry registry, string routePrefix, int pageLimit)
        : this(registry)
    {
        RoutePrefix += !string.IsNullOrEmpty(RoutePrefix) ? "/" : "";
        RoutePrefix += routePrefix;
        PageLimit = pageLimit;
    }

    public override void Build()
    {
        BuildEdm();
        ServiceRegistry.MergeServices(true);
    }

    public object EntitySet(Type entityType)
    {
        var ets = odataBuilder.EntitySets.FirstOrDefault(e => e.EntityType.ClrType == entityType);
        if (ets != null)
            return ets;              

        var entitySetName = entityType.Name;
        if (entityType.IsGenericType && entityType.IsAssignableTo(typeof(Identifier)))
            entitySetName = entityType.GetGenericArguments().FirstOrDefault().Name + "Identifier";

        var etc = odataBuilder.AddEntityType(entityType);
        etc.Name = entitySetName;
        ets = odataBuilder.AddEntitySet(entitySetName, etc);
        ets.EntityType.HasKey(entityType.GetProperty("Id"));

        SubEntitySet(entityType);

        return ets;
    }

    public object EntitySet<TDto>()
        where TDto : class
    {
        return odataBuilder.EntitySet<TDto>(typeof(TDto).Name);
    }

    public object AddInvocations(Type entityType)
    {
        var method = GetType().GetGenericMethod("AddInvocations");
        var methodInfo = method.MakeGenericMethod(entityType);
        return methodInfo.Invoke(this, Array.Empty<object>());
    }

    public void SubEntitySet(Type subEntityType)
    {
        subEntityType
            .GetProperties()
            .Select(p => p.PropertyType)
            .ForEach(pt =>
                pt.IsAssignableTo(typeof(IEnumerable)) ? pt.GetEnumerableElementType() : pt
            )
            .Where(p => p.IsAssignableTo(typeof(IContract)) || p.IsAssignableTo(typeof(IViewModel)))
            .ForEach(subType => EntitySet(subType))
            .Commit();
    }

    public IEdmModel GetEdmModel(bool disposeBuilder = false)
    {
        if(edmModel != null)
            return edmModel;

        odataBuilder.DataServiceVersion = new Version("4.0");
        odataBuilder.MaxDataServiceVersion = new Version("4.01");
        odataBuilder.BindingOptions = NavigationPropertyBindingOption.Auto;
        edmModel = odataBuilder.GetEdmModel();
        odataBuilder.ValidateModel(edmModel);
        if (disposeBuilder)
            odataBuilder = null;
        return edmModel;
    }

    public void BuildEdm()
    {
        Assembly[] asm = AppDomain.CurrentDomain.GetAssemblies();
        var controllerTypes = asm.SelectMany(a =>
                a.GetTypes()
                    .Where(type =>
                        type.GetCustomAttribute<DataOperatorAttribute>() != null
                        || type.GetCustomAttribute<ServiceOperatorAttribute>() != null
                        || type.GetCustomAttribute<RemoteDataOperatorAttribute>() != null
                        || type.GetCustomAttribute<RemoteServiceOperatorAttribute>() != null
                    )
            )
            .ToArray();

        var registry = new Registry<Type>();

        foreach (var controllerType in controllerTypes)
        {
            var genTypes = controllerType.BaseType.GenericTypeArguments;
            Type genType = null;
            if (
                genTypes.Length > 4
                && genTypes[1].IsAssignable(StoreType)
                && genTypes[2].IsAssignable(StoreType)
            )
            {
                genType = genTypes[4];
            }
            else if (genTypes.Length > 3)
            {
                genType = genTypes[3];
                if (
                    !genType.IsAssignableTo(typeof(IIdentifiable))
                    || !(genTypes[1].IsAssignable(StoreType) || genTypes[0].IsAssignable(StoreType))
                )
                    continue;
            }
            else if (genTypes.Length > 2)
            {
                genType = genTypes[2];
                if (
                    !genType.IsAssignableTo(typeof(IIdentifiable))
                    || !genTypes[0].IsAssignable(StoreType)
                )
                    continue;
            }
            if (genType == null)
                continue;

            EntitySet(genType);
            AddInvocations(genType);
            registry.Put(controllerType);            
        }

        DataServerRegistry.DataControllers.Put(StoreType, registry);
    }   

    public IMvcBuilder AddDataServicer(IMvcBuilder mvc)
    {        
        mvc.AddOData(b =>
        {
            b.RouteOptions.EnableQualifiedOperationCall = true;
            b.RouteOptions.EnableUnqualifiedOperationCall = true;
            b.RouteOptions.EnableKeyInParenthesis = true;
            b.RouteOptions.EnableControllerNameCaseInsensitive = true;
            b.RouteOptions.EnableActionNameCaseInsensitive = true;
            b.RouteOptions.EnablePropertyNameCaseInsensitive = true;
            b.RouteOptions.EnableKeyAsSegment = false;
            b.EnableContinueOnErrorHeader = true;
            b.EnableQueryFeatures(PageLimit)
                .AddRouteComponents(
                    RoutePrefix,
                    GetEdmModel(true),
                    ODataVersion.V401,
                    s =>
                    {
                        var defaultBatchHandler = new DefaultODataBatchHandler();
                        defaultBatchHandler.MessageQuotas.MaxNestingDepth = 5;
                        defaultBatchHandler.MessageQuotas.MaxOperationsPerChangeset = 20;

                        s.AddSingleton(
                                typeof(ODataPayloadValueConverter),
                                new CustomODataPayloadConverter()
                            )
                            .AddSingleton((sp) => defaultBatchHandler)
                            .AddScoped(_ => new ODataMessageWriterSettings
                            {
                                Validations = ValidationKinds.None,
                                EnableCharactersCheck = false,
                            })
                            .AddScoped(_ => new ODataMessageReaderSettings
                            {
                                Validations = ValidationKinds.None,
                                ReadUntypedAsString = false,
                                EnableUntypedCollections = false,
                            })
                            .AddSingleton<IStreamBasedJsonWriterFactory>(
                                new DefaultStreamBasedJsonWriterFactory(
                                    JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                                )
                            );
                    }
                );
        });       
        
        ServiceRegistry.MergeServices(true);
        return mvc;
    }

    public override IDataServerBuilder AddInvocations<TAuth>()
        where TAuth : class
    {
        SetFunctionAndAction<TAuth>();
        return base.AddInvocations<TAuth>();
    }

    private void SetFunctionAndAction<TAuth>()
        where TAuth : class
    {
        var name = typeof(TAuth).Name;

        var service = odataBuilder.EntitySet<TAuth>(name).EntityType.Collection.Action("Service");        
    }
}

public class CustomODataPayloadConverter : ODataPayloadValueConverter
{
    public override object ConvertFromPayloadValue(object value, IEdmTypeReference edmTypeReference)
    {
        if (edmTypeReference.PrimitiveKind() == EdmPrimitiveTypeKind.DateTimeOffset)
        {
            var str = value.ToString();
            if (str.Contains("0001"))
                return new DateTimeOffset(DateTime.Now);
            return new DateTimeOffset(DateTime.Parse(str));
        }

        if (edmTypeReference.IsEnum())
        {
            return value.ToString().ToTitleCase();
        }

        return base.ConvertFromPayloadValue(value, edmTypeReference);
    }
}
