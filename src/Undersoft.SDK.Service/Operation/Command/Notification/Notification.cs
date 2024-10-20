using MediatR;
using System.Text.Json;

namespace Undersoft.SDK.Service.Operation.Command.Notification;

using Command;
using Logging;
using Undersoft.SDK.Service.Data.Event;
using Undersoft.SDK.Service.Data.Object;
using Uniques;

public abstract class Notification<TCommand> : Event, INotification where TCommand : CommandBase
{
    public virtual OperationSite Site => OperationSite.Consumer;

    public TCommand Command { get; }

    public Notification() : base() { }

    protected Notification(TCommand command) : base()
    {
        var aggregateTypeFullName = command.Result.GetDataFullName();
        var eventTypeFullName = GetType().FullName;

        Command = command;
        Id = Unique.NewId;
        EntityId = command.Id;
        EntityTypeName = aggregateTypeFullName;
        TypeName = eventTypeFullName;
        var entity = (DataObject)command.Result;
        TypeName = entity.TypeName;
        Modifier = entity.Modifier;
        Modified = entity.Modified;
        Creator = entity.Creator;
        Created = entity.Created;
        PublishStatus = PublishStatus.Ready;
        PublishTime = Log.Clock;

        Data = JsonSerializer.SerializeToUtf8Bytes((CommandBase)command);
    }

    public Event GetEvent()
    {
        return this;
    }
}
