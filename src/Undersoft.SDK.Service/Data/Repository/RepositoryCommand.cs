﻿using System.Linq.Expressions;

namespace Undersoft.SDK.Service.Data.Repository;

using Quartz.Plugin.History;
using Series;
using Undersoft.SDK;
using Undersoft.SDK.Proxies;
using Undersoft.SDK.Service.Data.Store;
using Undersoft.SDK.Updating;

public abstract partial class Repository<TEntity> : IRepositoryCommand<TEntity> where TEntity : class, IOrigin, IInnerProxy
{
    public abstract TEntity NewEntry(params object[] parameters);

    public abstract TEntity Add(TEntity entity);

    public virtual IEnumerable<TEntity> Add(IEnumerable<TEntity> entity)
    {
        foreach (var e in entity)
            yield return Add(e);
    }

    public virtual IEnumerable<TEntity> Add(
        IEnumerable<TEntity> entities,
        Func<TEntity, Expression<Func<TEntity, bool>>> predicate
    )
    {
        var addins = entities;
        if (predicate != null)
            addins = entities.Where(e => !Query.Any(predicate.Invoke(e)));
        if (addins.Any())
        {
            foreach (var addin in addins)
            {
                yield return Add(addin);
            }
        }
    }

    public virtual IAsyncEnumerable<TEntity> AddAsync(IEnumerable<TEntity> entity)
    {
        return entity.ForEachAsync((e) => Add(e));
    }

    public virtual IAsyncEnumerable<TEntity> AddAsync(
        IEnumerable<TEntity> entities,
        Func<TEntity, Expression<Func<TEntity, bool>>> predicate
    )
    {
        var addin = entities;
        if (predicate != null)
            addin = entities.Where(e => !Query.Any(predicate.Invoke(e)));
        if (addin.Any())
            return AddAsync(addin);
        return null;
    }

    public virtual IAsyncEnumerable<TEntity> AddAsync(IAsyncEnumerable<TEntity> entity)
    {
        return entity.ForEachAsync((e) => Add(e));
    }

    public virtual TEntity Add(
        TEntity entity,
        Func<TEntity, Expression<Func<TEntity, bool>>> predicate
    )
    {
        if (predicate != null && Query.Any(predicate.Invoke(entity)))
            return null;
        return Add(entity);
    }

    public abstract TEntity Delete(TEntity entity);

    public virtual IEnumerable<TEntity> Delete(IEnumerable<TEntity> entity)
    {
        foreach (var e in entity)
            yield return Delete(e);
    }

    public virtual IEnumerable<TEntity> Delete(long[] ids)
    {
        var deck = Query.WhereIn(p => p.Id, ids).ToCatalog();
        foreach (TEntity model in deck)
        {
            yield return Delete(model);
        }
    }

    public virtual async Task<TEntity> Delete(params object[] key)
    {
        var toDelete = await Find(key);
        if (toDelete != null)
            return Delete(toDelete);
        return null;
    }

    public virtual TEntity Delete(Expression<Func<TEntity, bool>> predicate)
    {
        var toDelete = this[false, predicate];
        if (toDelete != null)
            return Delete(toDelete);
        return null;
    }

    public virtual TEntity Delete(
        TEntity entity,
        Func<TEntity, Expression<Func<TEntity, bool>>> predicate
    )
    {
        if (predicate != null)
            return Delete(predicate.Invoke(entity));
        return null;
    }

    public virtual IEnumerable<TEntity> Delete(
        IEnumerable<TEntity> entities,
        Func<TEntity, Expression<Func<TEntity, bool>>> predicate
    )
    {
        if (predicate != null)
            foreach (var entity in entities)
                yield return Delete(predicate.Invoke(entity));
        yield return null;
    }

    public virtual async IAsyncEnumerable<TEntity> DeleteAsync(IEnumerable<TEntity> entity)
    {
        foreach (var e in entity)
            yield return await Task.Run(() => Delete(e));
    }

    public virtual async IAsyncEnumerable<TEntity> DeleteAsync(
        IEnumerable<TEntity> entities,
        Func<TEntity, Expression<Func<TEntity, bool>>> predicate
    )
    {
        if (predicate != null)
            foreach (var entity in entities)
                yield return await Task.Run(() => Delete(predicate.Invoke(entity)));
        yield return null;
    }

    public virtual TEntity PatchBy<TModel>(TModel source, TEntity target) where TModel : class
    {
        return Stamp((TEntity)source.PatchTo(target.Proxy).Target);
    }

    public virtual TEntity PutBy<TModel>(TModel source, TEntity target) where TModel : class
    {
        return Stamp((TEntity)source.PutTo(target.Proxy).Target);
    }

    public virtual TEntity SetBy<TModel>(TModel source, TEntity target) where TModel : class
    {
        return PutBy(source, target);
    }

    public abstract TEntity Update(TEntity entity);

    public virtual async Task<TEntity> Set<TModel>(TModel entity, params object[] key)
        where TModel : class
    {
        if (key == null)
            return null;
        var _entity = await Find(key);
        if (_entity != null)
        {
            return SetBy(entity, _entity);
        }
        return null;
    }

    public virtual async Task<TEntity> Set<TModel>(
        TModel entity,
        object key,
        Func<TEntity, Expression<Func<TEntity, bool>>> condition
    ) where TModel : class, IOrigin
    {
        if (key == null)
            return null;
        var _entity = await Find(key);
        if (_entity != null)
        {
            if (condition != null && !Query.Any(condition.Invoke(_entity)))
                return null;

            return SetBy(entity, _entity);
        }
        return _entity;
    }

    public virtual async Task<TEntity> Set<TModel>(TModel entity) where TModel : class, IOrigin
    {
        if (entity.Id == 0)
            return null;
        var _entity = await Find(entity.Id);
        if (_entity != null)
            return PutBy(entity, _entity);
        return null;
    }

    public virtual IEnumerable<TEntity> Set<TModel>(IEnumerable<TModel> models)
        where TModel : class, IOrigin
    {
        var deck = Lookup<TModel>(models);

        foreach (var model in models)
        {
            if (deck.TryGet(model.Id, out TEntity entity))
            {
                yield return SetBy(model, entity);
            }
        }
    }

    public virtual async Task<TEntity> Set<TModel>(
        TModel entity,
        Func<TModel, Expression<Func<TEntity, bool>>> predicate,
        params Func<TModel, Expression<Func<TEntity, bool>>>[] conditions
    ) where TModel : class, IOrigin
    {
        return await Task.Run(async () =>
        {
            TEntity _entity = null;
            if (predicate != null)
                _entity = this[false, predicate(entity)];
            if (_entity == null)
                _entity = await Find(entity.Id);

            if (_entity == null)
                return null;

            if (conditions != null)
            {
                Expression<Func<TEntity, bool>> _condition = null;
                bool first = true;
                foreach (var condition in conditions)
                {
                    _condition = first ? condition(entity) : _condition.And(condition(entity));
                    first = false;
                }

                if (_condition != null && this[false, _condition] == null)
                    return null;
            }

            return SetBy(entity, _entity);
        });
    }

    public virtual IEnumerable<TEntity> Set<TModel>(
        IEnumerable<TModel> entities,
        Func<TModel, Expression<Func<TEntity, bool>>> predicate,
        params Func<TModel, Expression<Func<TEntity, bool>>>[] conditions
    ) where TModel : class, IOrigin
    {
        ISeries<TEntity> deck = null;
        if (predicate != null)
            deck = entities.Select(e => this[false, predicate(e)]).Where(e => e != null).ToListing();
        if (deck == null)
        {
            deck = Lookup<TModel>(entities).ToCatalog();
        }
        if (deck == null)
            yield return null;

        foreach (var entity in entities)
        {
            if (conditions != null)
            {
                Expression<Func<TEntity, bool>> _condition = null;
                bool first = true;
                foreach (var condition in conditions)
                {
                    _condition = first ? condition(entity) : _condition.And(condition(entity));
                    first = false;
                }

                if (_condition != null && this[false, _condition] == null)
                    yield return null;
            }

            yield return SetBy(entity, deck.Get(entity)
            );
        }
    }

    public virtual async IAsyncEnumerable<TEntity> SetAsync<TModel>(
        IEnumerable<TModel> entities,
        Func<TModel, Expression<Func<TEntity, bool>>> predicate,
        params Func<TModel, Expression<Func<TEntity, bool>>>[] conditions
    ) where TModel : class, IOrigin
    {
        ISeries<TEntity> deck = null;
        if (predicate != null)
            deck = entities.Select(e => this[false, predicate(e)]).Where(e => e != null).ToListing();
        if (deck == null)
        {
            deck = Lookup<TModel>(entities);
        }
        if (deck == null)
            yield return null;

        foreach (var entity in entities)
        {
            if (conditions != null)
            {
                Expression<Func<TEntity, bool>> _condition = null;
                bool first = true;
                foreach (var condition in conditions)
                {
                    _condition = first ? condition(entity) : _condition.And(condition(entity));
                    first = false;
                }

                if (_condition != null && this[false, _condition] == null)
                    yield return null;
            }
            yield return await Task.Run(
                () =>
                    SetBy(entity, deck.Get(entity)));
        }
    }

    public virtual Task<TEntity> Set<TModel>(
        TModel delta,
        Func<TModel, Expression<Func<TEntity, bool>>> predicate
    ) where TModel : class, IOrigin
    {
        return Task.Run(() =>
        {
            TEntity entity = null;
            if (predicate != null)
                entity = this[false, predicate(delta)];
            else
                entity = this[new object[] { delta.Id }];

            if (entity != null)
            {
                return SetBy(delta, entity);
            }
            return default;
        });
    }

    public virtual async IAsyncEnumerable<TEntity> SetAsync<TModel>(IEnumerable<TModel> models)
        where TModel : class, IOrigin
    {
        var deck = Lookup<TModel>(models);

        foreach (var model in models)
        {
            if (deck.TryGet(model.Id, out TEntity entity))
            {
                yield return await Task.Run(
                    () => SetBy(model, entity)
                );
            }
        }
    }

    public abstract TEntity Patch(TEntity entity);

    public virtual async Task<TEntity> Patch<TModel>(TModel delta) where TModel : class, IOrigin
    {
        if (delta.Id == 0)
            return null;
        var entity = await Find(delta.Id);
        if (entity != null)
        {
            return PatchBy(delta, entity);
        }
        return null;
    }

    public virtual ISeries<TEntity> Lookup<TModel>(IEnumerable<TModel> entities)
        where TModel : class, IOrigin
    {
        var listing = entities.ToListing();
        
        var items = listing.ForEach(e => cache.Lookup<TEntity>(e.Id)).Where(e => e != null).ToListing();
        
        if (items.Any())
            items.ForEach(item => ((IDataStoreContext)InnerContext).Attach(item)).Commit();
      
        if (items.Count < listing.Count)
            items.Add(
                this[
                    Query.WhereIn(
                        p => p.Id,
                        listing.Where(id => !items.ContainsKey(id)).Select(id => id.Id)
                    )
                ]
            );
        return items;
    }

    public virtual TEntity Lookup<TModel>(TModel entity) where TModel : class, IOrigin
    {
        var item = cache.Lookup<TEntity>(entity.Id);
        if (item != null)
            ((IDataStoreContext)InnerContext).Attach(item);
        return item;
    }

    public virtual IEnumerable<TEntity> Patch<TModel>(
        IEnumerable<TModel> entities,
        params Expression<Func<TEntity, object>>[] expanders
    ) where TModel : class, IOrigin
    {
        ISeries<TEntity> deck = null;
        if (expanders.Any())
        {
            deck = this[Query.WhereIn(p => p.Id, entities.Select(e => e.Id)), expanders].ToListing();
        }
        else
        {
            deck = Lookup<TModel>(entities);
        }
        return entities.ForOnly(
            entity => deck.ContainsKey(entity.Id),
            entity => PatchBy(entity, deck[entity.Id])
        ).Commit();
    }

    public async virtual Task<TEntity> Patch<TModel>(TModel delta, params object[] keys)
        where TModel : class, IOrigin
    {
        return await Task.Run(async () =>
        {
            if (keys == null)
                return null;

            var entity = await Find(keys);

            if (entity != null)
                return PatchBy(delta, entity);

            return null;
        });
    }

    public async virtual Task<TEntity> Patch<TModel>(
        TModel delta,
        Func<TModel, Expression<Func<TEntity, bool>>> predicate
    ) where TModel : class, IOrigin
    {
        return await Task.Run(() =>
        {
            TEntity entity = Lookup<TModel>(delta);
            if (entity == null || predicate != null && !entity.ToQueryable().Where(predicate(delta)).Commit().Any())
            {
                if (predicate != null)
                    entity = this[false, predicate(delta)];
                else
                    entity = this[new object[] { delta.Id }];
            }
            if (entity != null)
            {
                return PatchBy(delta, entity);
            }
            return default;
        });
    }

    public virtual IEnumerable<TEntity> Patch<TModel>(
        IEnumerable<TModel> entities,
        Func<TModel, Expression<Func<TEntity, bool>>> predicate,
        params Expression<Func<TEntity, object>>[] expanders
    ) where TModel : class, IOrigin
    {
        ISeries<TEntity> deck = null;
        if (predicate != null)
            if (expanders.Any())
                deck = entities.Select(e => this[false, predicate(e), expanders]).Where(e => e != null).ToListing();
            else
                deck = entities.Select(e => this[false, predicate(e)]).Where(e => e != null).ToListing();
        else if (expanders.Any())
            deck = this[
                Query.WhereIn(q => q.Id, entities.Select(i => i.Id)),
                expanders
            ].ToListing();

        if (deck == null)
        {
            foreach (var item in Patch(entities))
                yield return item;
        }

        foreach (var entity in entities)
        {
            if (deck.TryGet(entity.Id, out TEntity _entity))
                yield return PatchBy(entity, _entity);
        }
    }

    public virtual async IAsyncEnumerable<TEntity> PatchAsync<TModel>(
        IEnumerable<TModel> entities,
        params Expression<Func<TEntity, object>>[] expanders
    ) where TModel : class, IOrigin
    {
        ISeries<TEntity> deck = null;
        if (expanders.Any())
            deck = this[
                Query.WhereIn(p => p.Id, entities.Select(e => e.Id)),
                expanders
            ].ToCatalog();
        else
        {
            deck = Lookup<TModel>(entities);
        }

        foreach (var entity in entities)
        {
            if (deck.TryGet(entity.Id, out TEntity _entity))
            {
                yield return await Task.Run(
                    () => PatchBy(entity, _entity)
                );
            }
        }
    }

    public virtual async IAsyncEnumerable<TEntity> PatchAsync<TModel>(
        IEnumerable<TModel> entities,
        Func<TModel, Expression<Func<TEntity, bool>>> predicate,
        params Expression<Func<TEntity, object>>[] expanders
    ) where TModel : class, IOrigin
    {
        ISeries<TEntity> deck = null;
        if (predicate != null)
            if (expanders.Any())
                deck = entities.Select(e => this[false, predicate(e), expanders]).Where(e => e != null).ToListing();
            else
                deck = entities.Select(e => this[false, predicate(e)]).Where(e => e != null).ToListing();
        else if (expanders.Any())
            deck = this[
                Query.WhereIn(q => q.Id, entities.Select(i => i.Id)),
                expanders
            ].ToListing();

        if (deck == null)
        {
            foreach (var item in Patch(entities))
                yield return item;
        }

        foreach (var entity in entities)
        {
            if (deck.TryGet(entity.Id, out TEntity _entity))
                yield return await Task.Run(
                    () => PatchBy(entity, _entity)
                );
        }
    }

    public virtual Task<TEntity> Put(
        TEntity entity,
        Func<TEntity, Expression<Func<TEntity, bool>>> predicate,
        params Func<TEntity, Expression<Func<TEntity, bool>>>[] conditions
    )
    {
        return Task.Run(async () =>
        {
            TEntity _entity = null;
            if (predicate != null)
                _entity = this[false, predicate(entity)];

            if (_entity == null)
                _entity = await Find(entity.Id);

            if (conditions != null)
            {
                Expression<Func<TEntity, bool>> _condition = null;
                bool first = true;
                foreach (var condition in conditions)
                {
                    _condition = first ? condition(entity) : _condition.And(condition(entity));
                    first = false;
                }

                if (_condition != null && this[false, _condition] == null)
                    return null;
            }

            if (_entity == null)
                return Add(entity);

            return PutBy(entity, _entity);
        });
    }

    public virtual IEnumerable<TEntity> Put(
        IEnumerable<TEntity> entities,
        Func<TEntity, Expression<Func<TEntity, bool>>> predicate,
        params Func<TEntity, Expression<Func<TEntity, bool>>>[] conditions
    )
    {
        ISeries<TEntity> deck = null;
        if (predicate != null)
                deck = entities.Select(e => this[false, predicate(e)]).Where(e => e != null).ToListing();

        if (deck == null)
        {
            deck = Lookup<TEntity>(entities);
        }

        foreach (var entity in entities)
        {
            if (conditions != null)
            {
                Expression<Func<TEntity, bool>> _condition = null;
                bool first = true;
                foreach (var condition in conditions)
                {
                    _condition = first ? condition(entity) : _condition.And(condition(entity));
                    first = false;
                }

                if (_condition != null && this[false, _condition] == null)
                    yield return null;
            }

            if (deck.TryGet(entity.Id, out TEntity settin))
            {
                yield return PutBy(entity, settin);
            }
            else
                yield return Add(entity);
        }
    }

    public virtual async IAsyncEnumerable<TEntity> PutAsync(
        IEnumerable<TEntity> entities,
        Func<TEntity, Expression<Func<TEntity, bool>>> predicate,
        params Func<TEntity, Expression<Func<TEntity, bool>>>[] conditions
    )
    {
        ISeries<TEntity> deck = null;
        if (predicate != null)
        {
            deck = entities.Select(e => this[false, predicate(e)]).Where(e => e != null).ToListing();
        }
        if (deck == null)
        {
            deck = Lookup<TEntity>(entities);
        }

        foreach (var entity in entities)
        {
            if (conditions != null)
            {
                Expression<Func<TEntity, bool>> _condition = null;
                bool first = true;
                foreach (var condition in conditions)
                {
                    _condition = first ? condition(entity) : _condition.And(condition(entity));
                    first = false;
                }

                if (_condition != null && this[false, _condition] == null)
                    yield return null;
            }

            if (deck.TryGet(entity.Id, out TEntity settin))
            {
                yield return await Task.Run(
                    () => PutBy(entity, settin)
                );
            }
            else
                yield return await Task.Run(() => Add(entity));
        }
    }
}
