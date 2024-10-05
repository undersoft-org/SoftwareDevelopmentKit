using Undersoft.SDK.Service.Data.Client;

namespace Undersoft.SDK.Service.Data.Repository.Client
{
    public interface IRepositoryClients : ISeries<IRepositoryClient>
    {
        IRepositoryClient this[DataClientContext context] { get; set; }
        IRepositoryClient this[string contextName] { get; set; }
        IRepositoryClient this[Type contextType] { get; set; }

        IRepositoryClient<TContext> Add<TContext>(IRepositoryClient<TContext> repoSource) where TContext : DataClientContext;
        IRepositoryClient Get(Type contextType);
        IRepositoryClient<TContext> Get<TContext>() where TContext : DataClientContext;
        long GetKey(IRepositoryClient item);
        IRepositoryClient New(Type contextType, Uri route);
        IRepositoryClient<TContext> New<TContext>(Uri route) where TContext : DataClientContext;
        int PoolCount(Type contextType);
        int PoolCount<TContext>() where TContext : DataClientContext;
        IRepositoryClient<TContext> Put<TContext>(IRepositoryClient<TContext> repoSource) where TContext : DataClientContext;
        bool Remove<TContext>() where TContext : DataClientContext;
        bool TryAdd(Type contextType, IRepositoryClient repoSource);
        bool TryAdd<TContext>(IRepositoryClient<TContext> repoSource) where TContext : DataClientContext;
        bool TryGet(Type contextType, out IRepositoryClient repoSource);
        bool TryGet<TContext>(out IRepositoryClient<TContext> repoSource) where TContext : DataClientContext;
    }
}