using FluentValidation.Results;

using System.Text.Json.Serialization;
using Undersoft.SDK.Service.Data.Event;

namespace Undersoft.SDK.Service.Operation.Remote.Command;

public abstract class RemoteCommandBase : IRemoteCommand
{
    protected RemoteCommandBase()
    {
        Validation = new ValidationResult();
    }

    protected RemoteCommandBase(OperationKind commandMode, PublishMode publishMode) : this()
    {
        Kind = commandMode;
        Mode = publishMode;
    }

    protected RemoteCommandBase(object entryData, OperationKind commandMode, PublishMode publishMode)
        : this(commandMode, publishMode)
    {
        Model = entryData;
    }

    protected RemoteCommandBase(
        object entryData,
        OperationKind commandMode,
        PublishMode publishMode,
        params object[] keys
    ) : this(commandMode, publishMode, keys)
    {
        Model = entryData;
    }

    protected RemoteCommandBase(
        OperationKind commandMode,
        PublishMode publishMode,
        params object[] keys
    ) : this(commandMode, publishMode)
    {
        Keys = keys;
    }

    public OperationSite Site => OperationSite.Client;

    private IOrigin contract;

    public virtual long Id { get; set; }

    public object[] Keys { get; set; }

    [JsonIgnore]
    public virtual IOrigin Result
    {
        get => contract;
        set
        {
            contract = value;
            if (Id == 0 && contract.Id != 0)
                Id = contract.Id;
        }
    }

    [JsonIgnore]
    public virtual object Model { get; set; }

    [JsonIgnore]
    public ValidationResult Validation { get; set; }

    public string ErrorMessages => Validation.ToString();

    public OperationKind Kind { get; set; }

    public PublishMode Mode { get; set; }

    public virtual object Input => Model;

    public virtual object Output => IsValid ? Id : ErrorMessages;

    public bool IsValid => Validation.IsValid;
}
