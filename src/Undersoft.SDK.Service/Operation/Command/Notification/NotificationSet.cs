using MediatR;

namespace Undersoft.SDK.Service.Operation.Command.Notification;

using Command;
using Series;
using Undersoft.SDK.Service.Data.Event;

public abstract class NotificationSet<TCommand> : Listing<Notification<TCommand>>, INotification
    where TCommand : CommandBase
{
    public PublishMode PublishMode { get; set; }

    public NotificationSet() : base() { }

    protected NotificationSet(PublishMode publishPattern, Notification<TCommand>[] commands)
        : base(commands)
    {
        PublishMode = publishPattern;
    }
}
