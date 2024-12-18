﻿using MediatR;

namespace Undersoft.SDK.Service.Operation.Remote.Command.Notification;

using Command;
using Series;
using Undersoft.SDK.Service.Data.Event;

public abstract class RemoteNotificationSet<TCommand> : Catalog<RemoteNotification<TCommand>>, INotification
    where TCommand : RemoteCommandBase
{
    public PublishMode PublishMode { get; set; }

    protected RemoteNotificationSet(PublishMode publishPattern, RemoteNotification<TCommand>[] commands)
        : base(commands)
    {
        PublishMode = publishPattern;
    }
}
