namespace Undersoft.SDK.Service.Operation.Remote.Query;

using FluentValidation.Results;
using Undersoft.SDK.Proxies;
using Undersoft.SDK.Service.Data.Event;
using Undersoft.SDK.Service.Data.Query;

public abstract class RemoteQueryBase : IRemoteQuery
{
    protected IQueryParameters _parameters;

    public RemoteQueryBase()
    {
        Validation = new ValidationResult();
    }

    public RemoteQueryBase(OperationKind type) : this()
    {
        Kind = type;
    }

    public RemoteQueryBase(OperationKind type, object[] keys) : this(type)
    {
        Keys = keys;
    }

    public RemoteQueryBase(OperationKind type, IQueryParameters parameters) : this(type)
    {
        _parameters = parameters;
    }

    public OperationSite Site => OperationSite.Client;

    public virtual int Offset
    {
        get => Parameters.Offset;
        set => Parameters.Offset = value;
    }

    public virtual int Limit
    {
        get => Parameters.Limit;
        set => Parameters.Limit = value;
    }

    public virtual int Count
    {
        get => Parameters.Count;
        set => Parameters.Count = value;
    }

    public virtual ValidationResult Validation { get; set; }

    public bool IsValid => Validation.IsValid;

    public string ErrorMessages => Validation.ToString();

    public bool IsSingle => SingleResult != null;

    public virtual IQueryParameters Parameters
    {
        get => _parameters ??= new QueryParameters();
        set => _parameters = value;
    }

    public virtual IQueryable Result { get; set; }

    public virtual IInnerProxy SingleResult { get; set; }

    public virtual object[] Keys { get; set; }

    public virtual object Input => Parameters;

    public virtual object Output =>
        IsValid
            ? IsSingle
                ? SingleResult
                : (object)Result
            : (object)ErrorMessages;

    public OperationKind Kind { get; set; }

    public PublishMode Mode { get; set; }

    public virtual object Data
    {
        get => Parameters.Data;
        set => Parameters.Data = value;
    }
}
