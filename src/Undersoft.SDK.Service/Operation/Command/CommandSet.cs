using FluentValidation.Results;
using MediatR;

namespace Undersoft.SDK.Service.Operation.Command;

using Series;
using Undersoft.SDK;
using Undersoft.SDK.Proxies;
using Undersoft.SDK.Service.Data.Event;

public class CommandSet<TDto>
    : Listing<Command<TDto>>,
        IRequest<CommandSet<TDto>>,
        ICommandSet<TDto> where TDto : class, IOrigin, IInnerProxy
{
    public OperationSite Site => OperationSite.Server;

    public OperationKind Kind { get; set; }

    public PublishMode Mode { get; set; }

    protected CommandSet() : base() { }

    protected CommandSet(OperationKind commandMode) : base()
    {
        Kind = commandMode;
    }

    protected CommandSet(
        OperationKind commandMode,
        PublishMode publishPattern,
        Command<TDto>[] commands
    ) : base(commands)
    {
        Kind = commandMode;
        Mode = publishPattern;
    }

    public IEnumerable<Command<TDto>> Commands
    {
        get => AsValues();
    }

    public ValidationResult Validation { get; set; } = new ValidationResult();

    public object Input => Commands.Select(c => c.Contract);

    public object Output => Commands.ForEach(c => c.Validation.IsValid ? c.Id as object : c.Validation);

    IEnumerable<ICommand> ICommandSet.Commands
    {
        get => this.Cast<ICommand>();
    }
}
