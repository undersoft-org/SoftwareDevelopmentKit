﻿using System.Linq.Expressions;
using Microsoft.OData.Client;

namespace Undersoft.SDK.Service.Data.Remote.Repository;

using Logging;
using Microsoft.EntityFrameworkCore;
using Proxies;
using Series;
using Undersoft.SDK.Service.Access;
using Undersoft.SDK.Service.Data.Client;
using Undersoft.SDK.Service.Data.Entity;
using Undersoft.SDK.Service.Data.Object;
using Undersoft.SDK.Service.Data.Repository;
using Undersoft.SDK.Service.Data.Repository.Client;
using Undersoft.SDK.Service.Data.Store;
using Undersoft.SDK.Updating;
using Undersoft.SDK.Utilities;

public class RemoteRepository<TStore, TEntity>
    : RemoteRepository<TEntity>,
        IRemoteRepository<TStore, TEntity>
    where TEntity : class, IOrigin, IInnerProxy
    where TStore : IDataServiceStore
{
    public RemoteRepository(IRepositoryContextPool<DataClient<TStore>> pool)
        : base(pool.ContextPool) { }

    public RemoteRepository(
        IRepositoryContextPool<DataClient<TStore>> pool,
        IEntityCache<TStore, TEntity> cache
    )
        : base(pool.ContextPool)
    {
        this.cache = cache;
    }

    public RemoteRepository(
        IRepositoryContextPool<DataClient<TStore>> pool,
        IEntityCache<TStore, TEntity> cache,
        IServicer servicer
    )
        : base(pool.ContextPool)
    {
        var authorization = servicer.GetService<IAuthorization>();
        if (authorization.Credentials.SessionToken != null)
            SetAuthorization(authorization.Credentials.SessionToken);
        this.cache = cache;
    }

    public RemoteRepository(
        IRepositoryContextPool<DataClient<TStore>> pool,
        IEntityCache<TStore, TEntity> cache,
        IServicer servicer,
        IEnumerable<IRemoteProperty<IDataStore, TEntity>> remoteProps,
        IRemoteSynchronizer synchronizer
    )
        : this(pool, cache, servicer)
    {
        synchronizer.AddRepository(this);
        RemoteProperties = remoteProps.DoEach(
            (o) =>
            {
                o.Host = this;
                return o;
            }
        );
    }

    public override Task<int> Save(bool asTransaction, CancellationToken token = default)
    {
        return ContextLease.Save(asTransaction, token);
    }
}

public partial class RemoteRepository<TEntity> : Repository<TEntity>, IRemoteRepository<TEntity>
    where TEntity : class, IOrigin, IInnerProxy
{
    protected DataClientContext RemoteContext => (DataClientContext)InnerContext;
    protected DataServiceQuery<TEntity> RemoteQuery;

    public RemoteRepository() { }

    public RemoteRepository(IRepositoryClient repositorySource)
        : base(repositorySource)
    {
        if (RemoteContext != null)
        {
            RemoteQuery = RemoteContext.CreateQuery<TEntity>(Name);
            Expression = Expression.Constant(this);
            Provider = new RemoteRepositoryExpressionProvider<TEntity>(Query);
        }
    }

    public RemoteRepository(DataClientContext context)
        : base(context)
    {
        if (RemoteContext != null)
        {
            RemoteQuery = RemoteContext.CreateQuery<TEntity>(Name);
            Expression = Expression.Constant(this);
            Provider = new RemoteRepositoryExpressionProvider<TEntity>(RemoteQuery);
        }
    }

    public RemoteRepository(IRepositoryContextPool context)
        : base(context)
    {
        if (RemoteContext != null)
        {
            RemoteQuery = RemoteContext.CreateQuery<TEntity>(Name);
            Expression = Expression.Constant(this);
            Provider = new RemoteRepositoryExpressionProvider<TEntity>(Query);
        }
    }

    public RemoteRepository(IQueryProvider provider, Expression expression)
    {
        ElementType = typeof(TEntity).GetDataType();
        Provider = provider;
        Expression = expression;
    }

    public DataClientContext Context => RemoteContext;
    public override string Name => Context.GetMappedName(ElementType);
    public override string FullName => Context.GetMappedFullName(ElementType);
    public override DataServiceQuery<TEntity> Query =>
        RemoteContext.CreateQuery<TEntity>(Name, true);

    public string KeyString(params object[] keys)
    {
        return $"{Name}({(keys.Length > 1 ? keys.Aggregate(string.Empty, (a, b) => $"{a},{b}") : keys[0])})";
    }

    public override TEntity this[params object[] keys]
    {
        get => Lookup(keys);
        set
        {
            TEntity entity = Lookup(keys);
            if (entity != null)
            {
                PatchBy(value, entity);
            }
            else
                Add(value);
        }
    }
    
    public override TEntity this[
        object[] keys,
        params Expression<Func<TEntity, object>>[] expanders
    ]
    {
        get
        {
            DataServiceQuerySingle<TEntity> query = FindQuerySingle(keys);
            if (expanders != null)
                foreach (Expression<Func<TEntity, object>> expander in expanders)
                    query = query.Expand(expander);

            return query.GetValue();
        }
        set
        {
            DataServiceQuerySingle<TEntity> query = FindQuerySingle(keys);
            if (expanders != null)
                if (expanders != null)
                    foreach (Expression<Func<TEntity, object>> expander in expanders)
                        query = query.Expand(expander);

            TEntity entity = query.GetValue();
            if (entity != null)
            {
                PatchBy(value, entity);
            }
            else
            {
                Add(value);
            }
        }
    }
    
    public override object this[
        Expression<Func<TEntity, object>> selector,
        object[] keys,
        params Expression<Func<TEntity, object>>[] expanders
    ]
    {
        get
        {
            DataServiceQuerySingle<TEntity> query = FindQuerySingle(keys);
            if (expanders != null)
                foreach (Expression<Func<TEntity, object>> expander in expanders)
                    query = query.Expand(expander);

            return query.Select(selector);
        }
        set
        {
            DataServiceQuerySingle<TEntity> query = FindQuerySingle(keys);
            if (expanders != null)
                foreach (Expression<Func<TEntity, object>> expander in expanders)
                    query = query.Expand(expander);

            if (query != null)
            {
                object item = query.Select(selector);
                RemoteContext.Command(CommandType.PATCH, value.PatchTo(item), Name);
            }
        }
    }
    
    public override IQueryable<TEntity> this[Expression<Func<TEntity, object>>[] expanders]
    {
        get
        {
            DataServiceQuery<TEntity> query = Query;
            if (expanders != null)
            {
                foreach (var expander in expanders)
                {
                    query = query.Expand(expander);
                }
            }

            return query;
        }
    }

    public override ISeries<TEntity> Lookup<TModel>(IEnumerable<TModel> entities)
    {
        var dtos = entities.ToListing();
        var deck = dtos.ForEach(e => cache.Lookup<TEntity>(e)).Where(i => i != null).ToListing();
        if (deck.Count < dtos.Count)
            deck.Add(
                this[
                    Query.WhereIn(
                        p => p.Id,
                        dtos.Where(id => !deck.ContainsKey(id)).Select(id => id.Id)
                    )
                ]
            );

        return deck;
    }

    public override TEntity Lookup<TModel>(TModel entity)
    {
        return Lookup(entity.Id);
    }

    private TEntity Lookup(object key)
    {
        var item = cache.Lookup<TEntity>(key);
        if (item == null)
            item = FindQuerySingle(key).GetValue();
        return item;
    }

    private TEntity Lookup(params object[] keys)
    {
        var item = cache.Lookup<TEntity>(keys);
        if (item == null)
            item = FindQuerySingle(keys).GetValue();
        return item;
    }

    private async Task<TEntity> LookupAsync(params object[] keys)
    {
        var item = cache.Lookup<TEntity>(keys);
        if (item == null)
            item = await FindQuerySingle(keys).GetValueAsync();
        return item;
    }

    public override TEntity NewEntry(params object[] parameters)
    {
        TEntity entity = Sign(typeof(TEntity).New<TEntity>(parameters));
        Add(entity);
        return entity;
    }

    public override TEntity Patch(TEntity entity)
    {
        if (entity != null)
        {
            entity = Stamp(entity);
            RemoteContext.Command(CommandType.PATCH, entity, Name);
            return entity;
        }
        return null;
    }

    public override TEntity PatchBy<TModel>(TModel source, TEntity target)
        where TModel : class
    {
        return Patch((TEntity)source.PatchTo(target.Proxy).Target);
    }

    public override async Task<TEntity> PatchBy<TModel>(TModel model)
    {
        return await Task.FromResult(Patch(model.PutTo<TEntity>()));
    }

    public override TEntity PutBy<TModel>(TModel source, TEntity target)
        where TModel : class
    {
        return Update((TEntity)source.PutTo(target.Proxy).Target);
    }

    public override IEnumerable<TEntity> Put(
        IEnumerable<TEntity> entities,
        Func<TEntity, Expression<Func<TEntity, bool>>> predicate,
        params Func<TEntity, Expression<Func<TEntity, bool>>>[] conditions
    )
    {
        ISeries<TEntity> deck = null;
        if (predicate != null)
        {
            deck = entities.SelectMany(e => Query.Where(predicate(e))).ToRegistry();
        }
        else
        {
            deck = Lookup<TEntity>(entities);
        }

        foreach (var entity in entities)
        {
            foreach (var condition in conditions)
                if (!Query.Any(condition(entity)))
                    yield return null;

            if (deck.TryGet(entity.Id, out TEntity settin))
            {
                yield return PutBy(entity, settin);
            }
            else
                yield return Add(entity);
        }
    }

    public override TEntity SetBy<TModel>(TModel source, TEntity target)
        where TModel : class
    {
        return PutBy(source, target);
    }

    public override async Task<TEntity> SetBy<TModel>(TModel model)
    {
        return await Task.FromResult(Update(model.PutTo<TEntity>()));
    }

    public override TEntity Delete(TEntity entity)
    {
        RemoteContext.Command(CommandType.DELETE, entity, Name);
        return entity;
    }

    public override async Task<TEntity> DeleteBy<TModel>(TModel model)
    {
        return await Task.FromResult(Delete(model.PutTo<TEntity>()));
    }

    public override TEntity Update(TEntity entity)
    {
        if (entity != null)
        {
            entity = Stamp(entity);
            RemoteContext.Command(CommandType.PUT, entity, Name);
            return entity;
        }
        return null;
    }

    public override TEntity Add(TEntity entity)
    {
        if (entity != null)
        {
            entity = Sign(entity);
            RemoteContext.Command(CommandType.POST, entity, Name);
            return entity;
        }
        return null;
    }

    public override IEnumerable<TEntity> Add(IEnumerable<TEntity> entity)
    {
        foreach (TEntity e in entity)
            yield return Add(e);
    }

    public override IAsyncEnumerable<TEntity> AddAsync(IEnumerable<TEntity> entity)
    {
        return entity.ForEachAsync((e) => Add(e));
    }

    public override TEntity AddBy<TModel>(TModel model)
    {
        return Add(model.PutTo<TEntity>());
    }

    public override Task<TEntity> Find(params object[] keys)
    {
        return LookupAsync(keys);
    }

    public async Task<IEnumerable<TEntity>> FindMany(params object[] keys)
    {
        var _batch = new Chain<DataServiceRequest>();
        foreach (object key in keys)
        {
            if (key.GetType().IsAssignableTo(typeof(object[])))
                _batch.Put(key, RemoteContext.CreateQuery<TEntity>(KeyString((object[])key), true));
            else
                _batch.Put(key, RemoteContext.CreateQuery<TEntity>(KeyString(key), true));
        }
        return (await RemoteContext.ExecuteBatchAsync(_batch.ToArray())).SelectMany(o =>
            o as QueryOperationResponse<TEntity>
        );
    }

    public DataServiceQuerySingle<TEntity> FindQuerySingle(params object[] keys)
    {
        if (keys != null)
            return RemoteContext.CreateFunctionQuerySingle<TEntity>(KeyString(keys), null, true);
        return null;
    }

    public IQueryable<TEntity> FindQuery(object[] keys)
    {
        if (keys != null)
            return Query.WhereIn(e => e.Id, keys);
        return null;
    }

    public override IQueryable<TEntity> AsQueryable()
    {
        return Query;
    }

    public virtual async Task<ISeries<TEntity>> LoadAsync(int offset = 0, int limit = 0)
    {
        if (limit > 0)
        {
            return await Query.Skip(offset).Take(limit).ToListingAsync();
        }
        else
        {
            return await Query.ToListingAsync();
        }
    }

    public void SetAuthorization(string token)
    {
        RemoteContext.SetAuthorization(token);
    }

    protected override async Task<int> SaveAsTransaction(CancellationToken token = default)
    {
        try
        {
            return (
                await RemoteContext.SaveChangesAsync(
                    SaveChangesOptions.BatchWithSingleChangeset,
                    token
                )
            ).Count();
        }
        catch (Exception e)
        {
            RemoteContext.Failure<Datalog>(
                $"{$"Fail on update dataservice as singlechangeset, using context:{RemoteContext.GetType().Name}, "}{$"TimeStamp: {DateTime.Now.ToString()}"}",
                ex: e
            );
        }

        return -1;
    }

    protected override async Task<int> SaveChanges(CancellationToken token = default)
    {
        try
        {
            return (
                await RemoteContext.SaveChangesAsync(
                    SaveChangesOptions.BatchWithIndependentOperations
                        | SaveChangesOptions.ContinueOnError,
                    token
                )
            ).Count();
        }
        catch (Exception e)
        {
            RemoteContext.Failure<Datalog>(
                $"{$"Fail on update dataservice as independent operations, using context:{RemoteContext.GetType().Name}, "}{$"TimeStamp: {DateTime.Now.ToString()}"}",
                ex: e
            );
        }

        return -1;
    }

    public override object TracePatching(
        object source,
        string propertyName,
        object target = null,
        Type type = null
    )
    {
        if (type == null)
        {
            RemoteContext.AttachTo(target.GetType().Name, target);
            return target;
        }
        else
        {
            var proxy = (IProxy)source;
            target = proxy[propertyName];
            if (target == null)
                proxy[propertyName] = target = type.New();
            return target;
        }
    }

    public override object TraceAdding(
        object source,
        string propertyName,
        object target = null,
        Type type = null
    )
    {
        if (target == null)
        {
            if (
                !RemoteContext
                    .EntityTracker.Entities.Select(e => ((IIdentifiable)e.Entity).Id)
                    .Contains(((IIdentifiable)((IProxy)source).Target).Id)
            )
                target = ((IProxy)source)[propertyName];
            if (target == null)
                ((IProxy)source)[propertyName] = target = type.New();
        }
        return target;
    }
}
