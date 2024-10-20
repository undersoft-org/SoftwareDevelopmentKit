using FluentValidation.Results;
using MediatR;
using Undersoft.SDK.Service.Data.Event;

namespace Undersoft.SDK.Service.Operation.Remote.Command;

public class RemoteCommandSet<TModel>
    : Registry<RemoteCommand<TModel>>,
        IRequest<RemoteCommandSet<TModel>>,
        IRemoteCommandSet<TModel> where TModel : class, IOrigin, IInnerProxy
{
    public OperationSite Site => OperationSite.Client;

    public OperationKind Kind { get; set; }

    public PublishMode Mode { get; set; }

    protected RemoteCommandSet() : base(true) { }

    protected RemoteCommandSet(OperationKind commandMode) : base()
    {
        Kind = commandMode;
    }

    protected RemoteCommandSet(
        OperationKind commandMode,
        PublishMode publishPattern,
        RemoteCommand<TModel>[] DtoCommands
    ) : base(DtoCommands)
    {
        Kind = commandMode;
        Mode = publishPattern;
    }

    public IEnumerable<RemoteCommand<TModel>> Commands
    {
        get => AsValues();
    }

    public ValidationResult Validation { get; set; } = new ValidationResult();

    public object Input => Commands.Select(c => c.Model);

    public object Output => Commands.ForEach(c => c.Validation.IsValid ? c.Id as object : c.Validation);

    public Delegate Processings { get; set; }

    IEnumerable<IRemoteCommand> IRemoteCommandSet.Commands
    {
        get => this.Cast<IRemoteCommand>();
    }
}
