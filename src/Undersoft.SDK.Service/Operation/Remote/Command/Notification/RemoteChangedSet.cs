﻿using System.Linq.Expressions;
using System.Text.Json.Serialization;

namespace Undersoft.SDK.Service.Operation.Remote.Command.Notification;

using Command;
using Undersoft.SDK.Proxies;
using Undersoft.SDK.Service.Data.Store;

public class RemoteChangedSet<TStore, TDto, TModel> : RemoteNotificationSet<RemoteCommand<TModel>>
    where TDto : class, IOrigin, IInnerProxy
    where TModel : class, IOrigin, IInnerProxy
    where TStore : IDataServiceStore
{
    [JsonIgnore]
    public Func<TModel, Expression<Func<TDto, bool>>> Predicate { get; }

    public RemoteChangedSet(RemoteChangeSet<TStore, TDto, TModel> commands)
        : base(
            commands.Mode,
            commands
                .ForOnly(
                    c => c.Result != null,
                    c => new RemoteChanged<TStore, TDto, TModel>((RemoteChange<TStore, TDto, TModel>)c)
                )
                .ToArray()
        )
    {
        Predicate = commands.Predicate;
    }
}
