﻿namespace Undersoft.SDK.Proxies;

using Rubrics;
using System;
using System.ComponentModel;
using System.Linq;
using Undersoft.SDK.Instant;
using Undersoft.SDK.Series;
using Undersoft.SDK.Utilities;

public class ProxyGenerator<T> : ProxyGenerator
{
    public ProxyGenerator() : base(typeof(T)) { }

    public ProxyGenerator(string proxyName) : base(typeof(T), proxyName) { }
}

public class ProxyGenerator : IInstantGenerator
{
    private ISeries<MemberBuilder> rubricModels;
    private MemberBuilderCreator rubricBuilder;
    private Type compiledType;

    public ProxyGenerator(Type figureModelType) : this(figureModelType, null) { }

    public ProxyGenerator(Type figureModelType, string figureTypeName)
    {        
        BaseType = figureModelType;

        Name = string.IsNullOrEmpty(figureTypeName)
            ? figureModelType.FullName
            : figureTypeName;

        if (figureModelType.IsGenericType)
            Name = Name.Split('`')[0];

        Name += "Proxy";

        rubricBuilder = new MemberBuilderCreator();
        rubricModels = rubricBuilder.Create(figureModelType);

        Rubrics = new MemberRubrics(rubricModels.Select(m => m.Member).ToArray());
        Rubrics.KeyRubrics = new MemberRubrics();
    }

    public Type BaseType { get; set; }

    public string Name { get; set; }

    public IRubrics Rubrics { get; set; }

    public int Size { get; set; }

    public Type Type { get; set; }

    public bool Traceable { get; set; }

    public object New()
    {
        return Generate();
    }

    public IProxy Generate(object obj = null)
    {
        var proxy = Compile();
        if (obj == null)
            obj = BaseType.New();
        proxy.Target = obj;
        return proxy;
    }

    protected IProxy Compile()
    {
        if (Type != null)
            return CreateInstance();

        try
        {
            IProxy proxy = Compile(new ProxyCompiler(this, rubricModels));
            Rubrics.Update();
            proxy.Rubrics = Rubrics;
            return proxy;
        }
        catch (Exception ex)
        {
            throw new ProxyCompilerException("ProxyGenerator compilation at runtime failed see inner exception", ex);
        }
    }

    private IProxy Compile(ProxyCompiler compiler)
    {
        var _compiler = compiler;

        compiledType = _compiler.CompileProxyType(Name);

        Rubrics.KeyRubrics.Add(_compiler.Identities.Values.Select(v => Rubrics[v.Name]).ToArray());

        var obj = compiledType.New();

        Type = obj.GetType();

        Rubrics.ForEach(
            (f, y) => new object[]
            {
                f.FieldId = y,
                f.RubricId = y
            }).ToArray();

        return (IProxy)obj;
    }

    private IProxy CreateInstance()
    {
        var s = (IProxy)Type.New();
        s.Rubrics = Rubrics;
        //s.Changes = new HashSet<string>();
        //s.PropertyChanged += OnPropertyChanged;
        return s;
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        ((IProxy)sender).Changes.Add(e.PropertyName);
    }
}

public class ProxyCompilerException : Exception
{
    public ProxyCompilerException(string message, Exception innerException) : base(message, innerException) { }
}