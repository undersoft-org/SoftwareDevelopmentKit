using FluentValidation.Results;
using System.Text.Json.Serialization;

namespace Undersoft.SDK.Service.Operation.Command;

using Undersoft.SDK;
using Undersoft.SDK.Service.Data.Event;

public abstract class CommandBase : ICommand
{
    private IOrigin entity;

    public virtual long Id { get; set; }

    public object[] Keys { get; set; }

    [JsonIgnore]
    public virtual IOrigin Result
    {
        get => entity;
        set
        {
            entity = value;
            if (Id == 0 && entity.Id != 0)
                Id = entity.Id;
        }
    }

    [JsonIgnore]
    public virtual object Contract { get; set; }

    [JsonIgnore]
    public ValidationResult Validation { get; set; }

    public string ErrorMessages => Validation.ToString();

    public virtual OperationSite Site => OperationSite.Server;

    public OperationKind Kind { get; set; }

    public PublishMode Mode { get; set; }

    public virtual object Input => Contract;

    public virtual object Output => IsValid ? Id : ErrorMessages;

    public bool IsValid => Validation.IsValid;

    protected CommandBase()
    {
        Validation = new ValidationResult();
    }

    protected CommandBase(OperationKind commandMode, PublishMode publishMode) : this()
    {
        Kind = commandMode;
        Mode = publishMode;
    }

    protected CommandBase(object entryData, OperationKind commandMode, PublishMode publishMode)
        : this(commandMode, publishMode)
    {
        Contract = entryData;
    }

    protected CommandBase(
        object entryData,
        OperationKind commandMode,
        PublishMode publishMode,
        params object[] keys
    ) : this(commandMode, publishMode, keys)
    {
        Contract = entryData;
    }

    protected CommandBase(
        OperationKind commandMode,
        PublishMode publishMode,
        params object[] keys
    ) : this(commandMode, publishMode)
    {
        Keys = keys;
    }
}
