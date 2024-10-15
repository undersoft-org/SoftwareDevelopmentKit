namespace Undersoft.SDK.Service.Data.Repository.Client;

using Configuration;
using Logging;
using Microsoft.OData.Edm;
using Series;
using Undersoft.SDK.Service.Access;
using Undersoft.SDK.Service.Data.Client;
using Undersoft.SDK.Service.Data.Object;
using Undersoft.SDK.Service.Data.Repository;
using Undersoft.SDK.Utilities;
using Uniques;

public class RepositoryClient : Registry<IRepositoryContext>, IRepositoryClient
{
    protected Uri uri;
    protected Uscn servicecode;
    private new bool disposedValue;
    protected Type contextType;

    public RepositoryClient()
    {
        Site = DataSite.Client;
    }

    public RepositoryClient(Type contextType, IServiceConfiguration config) : this()
    {
        var endpoint = config.Client(contextType.FullName);
        var connectionString = config.ClientConnectionString(contextType.FullName);
        PoolSize = config.ClientPoolSize(endpoint);
        InnerContext = CreateContext(contextType, new Uri(connectionString));
    }

    public RepositoryClient(Type contextType, Uri serviceRoot) : this()
    {
        InnerContext = CreateContext(contextType, serviceRoot);
    }

    public RepositoryClient(Type contextType, string connectionString)
        : this()
    {
        InnerContext = CreateContext(contextType, new Uri(connectionString));
    }

    public RepositoryClient(IRepositoryClient pool) : this()
    {
        PoolSize = pool.PoolSize;
        ContextPool = pool;
        InnerContext = pool.CreateContext();
    }

    public object InnerContext { get; set; }

    public DataSite Site { get; set; }

    public int PoolSize { get; set; }

    public Type ContextType => contextType ??= InnerContext.GetType();

    public virtual Uri Route => uri;

    public virtual DataClientContext Context => (DataClientContext)InnerContext;

    public virtual object CreateContext()
    {
        return ContextType.New<DataClientContext>(uri);
    }

    public virtual object CreateContext(Type contextType, Uri serviceRoot)
    {
        uri ??= serviceRoot;
        this.contextType ??= contextType;
        return (DataClientContext)contextType.New(uri);
    }

    public virtual TContext GetContext<TContext>() where TContext : DataClientContext
    {
        return (TContext)InnerContext;
    }

    public virtual TContext CreateContext<TContext>() where TContext : class
    {
        contextType ??= typeof(TContext);
        return typeof(TContext).New<TContext>(uri);
    }

    public virtual TContext CreateContext<TContext>(Uri serviceRoot)
        where TContext : DataClientContext
    {
        uri = serviceRoot;
        contextType ??= typeof(TContext);
        return typeof(TContext).New<TContext>(uri);
    }

    public async Task<IEdmModel> BuildMetadata()
    {
        var edmModel = await Context.CreateServiceModel().ConfigureAwait(false);
        Context.GetEdmEntityTypes();
        return edmModel;
    }

    public override long Id
    {
        get => servicecode.Id == 0 ? servicecode.Id = Unique.NewId : servicecode.Id;
        set => servicecode.Id = value;
    }

    public override long TypeId
    {
        get =>
            servicecode.TypeId == 0
                ? servicecode.TypeId = ContextType.GetDataTypeId()
                : servicecode.TypeId;
        set => servicecode.TypeId = value;
    }

    public IRepositoryContext ContextLease { get; set; }

    public IRepositoryContextPool ContextPool { get; set; }

    public bool Pooled => ContextPool != null;

    public bool Leased => ContextLease != null;

    protected override async void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
                base.Dispose(true);

            await Save(true).ConfigureAwait(false);

            InnerContext = null;
            contextType = null;
            uri = null;

            servicecode.Dispose();

            await ReleaseAsync().ConfigureAwait(false);

            disposedValue = true;
        }
    }

    public virtual IRepositoryContext Rent()
    {
        if (TryDequeue(out IRepositoryContext context))
        {
            return context;
        }

        return context = (IRepositoryContext)
            typeof(RepositoryClient<>).MakeGenericType(ContextType).New(this);
    }

    public virtual void Return()
    {
        ResetState();
        ContextPool.Add((IRepositoryContext)this);
    }

    public virtual async Task ReturnAsync(CancellationToken token = default)
    {
        await ResetStateAsync(token).ConfigureAwait(false);
        ContextPool.Add((IRepositoryContext)this);
    }

    public virtual void CreatePool()
    {
        Type repotype = typeof(RepositoryClient<>).MakeGenericType(ContextType);
        var size = PoolSize - Count;
        for (int i = 0; i < size; i++)
        {
            var repo = repotype.New<IRepositoryContext>(this);
            Add(repo);
        }
    }

    public virtual void CreatePool<TContext>()
    {
        Type repotype = typeof(RepositoryClient<>).MakeGenericType(typeof(TContext));
        var size = PoolSize - Count;
        for (int i = 0; i < size; i++)
        {
            var repo = repotype.New<IRepositoryContext>(this);
            repo.Id = Unique.NewId;
            Add(repo);
        }
    }

    public virtual void ResetState()
    {
        Context.Entities.ForEach((e) => Context.Detach(e.Entity));
    }

    public virtual async Task ResetStateAsync(CancellationToken token = default)
    {
        await Task.Factory.StartNew(() => ResetState(), token).ConfigureAwait(false);
    }

    public virtual Task<int> Save(bool asTransaction, CancellationToken token = default)
    {
        return saveClient(asTransaction);
    }

    public virtual bool Release()
    {
        if (Leased)
        {
            var destContext = ContextLease;
            destContext.ContextLease = null;
            destContext.InnerContext = null;
            destContext = null;
            ContextLease = null;

            Return();

            return true;
        }
        return false;
    }

    public virtual async Task<bool> ReleaseAsync(CancellationToken token = default)
    {
        if (Leased)
        {
            var destContext = ContextLease;
            destContext.ContextLease = null;
            destContext.InnerContext = null;
            destContext = null;
            ContextLease = null;

            await ReturnAsync(token).ConfigureAwait(false);

            return true;
        }
        return false;
    }

    public virtual void Lease(IRepositoryContext destContext)
    {
        if (Pooled)
        {
            var rentContext = Rent();

            rentContext.ContextLease = destContext;
            destContext.ContextLease = rentContext;
            destContext.InnerContext = rentContext.InnerContext;
        }
        else
        {
            if (Count > 0)
                destContext.ContextPool = this;
            destContext.InnerContext = CreateContext();
        }
        disposedValue = false;
    }

    public virtual async Task LeaseAsync(
        IRepositoryContext destContext,
        CancellationToken token = default
    )
    {
        await Task.Factory.StartNew(() => Lease(destContext), token).ConfigureAwait(false);
    }

    private async Task<int> saveClient(bool asTransaction, CancellationToken token = default)
    {
        if (asTransaction)
            return await saveAsTransaction(Context, token).ConfigureAwait(false);
        else
            return await saveChanges(Context, token).ConfigureAwait(false);
    }

    private async Task<int> saveAsTransaction(
        DataClientContext context,
        CancellationToken token = default
    )
    {
        try
        {
            return (await context.CommitChanges(true).ConfigureAwait(false)).Length;
        }
        catch (Exception e)
        {
            context.Failure<Datalog>(
                $"Fail on update dataservice as singlechangeset, using context:{context.GetType().Name}, "
                    + $"TimeStamp: {DateTime.Now.ToString()}",
                ex: e
            );
        }

        return -1;
    }

    private async Task<int> saveChanges(DataClientContext context, CancellationToken token = default)
    {
        try
        {
            return (await context.CommitChanges().ConfigureAwait(false)).Length;
        }
        catch (Exception e)
        {
            context.Failure<Datalog>(
                $"Fail on update dataservice as independent operations, using context:{context.GetType().Name}, "
                    + $"TimeStamp: {DateTime.Now.ToString()}",
                ex: e
            );
        }

        return -1;
    }

    public void SnapshotConfiguration()
    {
        throw new NotImplementedException();
    }
}
