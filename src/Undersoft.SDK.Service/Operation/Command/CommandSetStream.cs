using MediatR;

namespace Undersoft.SDK.Service.Operation.Command;

using Undersoft.SDK;
using Undersoft.SDK.Proxies;
using Undersoft.SDK.Service.Data.Event;

public class CommandSetStream<TDto>
    : CommandSet<TDto>,
        IStreamRequest<Command<TDto>>,
        ICommandSet<TDto> where TDto : class, IOrigin, IInnerProxy
{
    protected CommandSetStream() : base() { }

    protected CommandSetStream(OperationKind commandMode) : base(commandMode) { }

    protected CommandSetStream(
        OperationKind commandMode,
        PublishMode publishPattern,
        Command<TDto>[] DtoCommands
    ) : base(commandMode, publishPattern, DtoCommands) { }
}
