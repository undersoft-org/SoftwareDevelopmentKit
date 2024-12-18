﻿using Microsoft.OData.Client;
using Undersoft.SDK.Service.Data.Client;
using Undersoft.SDK.Service.Data.Repository;

namespace Undersoft.SDK.Service.Data.Remote.Repository;

public interface IRemoteRepository<TStore, TEntity> : IRemoteRepository<TEntity> where TEntity : class, IOrigin, IInnerProxy
{
}

public interface IRemoteRepository<TEntity> : IRepository<TEntity> where TEntity : class, IOrigin, IInnerProxy
{
    DataClientContext Context { get; }
    string FullName { get; }
    string Name { get; }

    void SetAuthorization(string securityString);

    Task<IEnumerable<TEntity>> FindMany(params object[] keys);
    IQueryable<TEntity> FindQuery(params object[] keys);
    DataServiceQuerySingle<TEntity> FindQuerySingle(params object[] keys);

    string KeyString(params object[] keys);

    Task<TModel> Service<TModel>(string method, TModel args);

    Task<ISeries<TEntity>> LoadAsync(int offset, int limit);
}