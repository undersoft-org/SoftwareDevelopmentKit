﻿namespace Undersoft.SDK.Service.Operation.Remote.Invocation.Notification;

using Undersoft.SDK;
using Undersoft.SDK.Service.Data.Store;
using Undersoft.SDK.Service.Operation.Invocation;
using Undersoft.SDK.Service.Operation.Remote.Invocation;

public class RemoteServiceInvoked<TStore, TService, TModel> : RemoteInvokeNotification<Invocation<TModel>>
    where TModel : class, IOrigin
    where TService : class
    where TStore : IDataServiceStore
{
    public RemoteServiceInvoked(RemoteService<TStore, TService, TModel> command) : base(command)
    {
    }
}
