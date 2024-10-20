using FluentValidation.Results;
using System.Text.Json.Serialization;
using Undersoft.SDK.Service.Data.Event;

namespace Undersoft.SDK.Service.Operation.Invocation;

public abstract class InvocationBase : IInvocation
{
    public virtual long Id { get; set; }

    public Arguments Arguments { get; set; }

    public virtual object Return { get; set; }

    public Delegate Processings { get; set; }

    [JsonIgnore]
    public virtual object Response { get; set; }

    [JsonIgnore]
    public ValidationResult Validation { get; set; }

    public string ErrorMessages => Validation.ToString();

    public PublishMode Mode {  get; set; }

    public OperationKind Kind { get; set; }

    public virtual OperationSite Site => OperationSite.Internal;

    public virtual object Input => Arguments;

    public virtual object Output => IsValid ? Response : ErrorMessages;

    public bool IsValid => Validation.IsValid;

    protected InvocationBase()
    {
        Validation = new ValidationResult();
    }

    protected InvocationBase(OperationKind commandMode) : this()
    {
        Kind = commandMode;
    }

    protected InvocationBase(OperationKind commandMode, Type serviceType, string method, object argument)
        : this(commandMode)
    {
        Arguments = new Arguments(method, argument, serviceType.FullName, serviceType);
    }

    protected InvocationBase(OperationKind commandMode, Type serviceType, string method, Arguments arguments)
        : this(commandMode)
    {
        Arguments = arguments;
        Arguments.TargetType = serviceType;
        Arguments.ForEach(a => a.TargetName = serviceType.FullName);       
    }

    protected InvocationBase(OperationKind commandMode, Type serviceType, string method, object[] arguments)
       : this(commandMode)
    {
        var args = new Arguments(serviceType);
        arguments.ForEach(a => args.New(a, method, serviceType.FullName));
        args.TargetType = serviceType;
        Arguments = args;
    }

    protected InvocationBase(OperationKind commandMode, Type serviceType, string method, byte[] binaries)
       : this(commandMode)
    {
        Arguments = new Arguments(method, binaries, serviceType.FullName, serviceType);
        Arguments.TargetType = serviceType;
    }

    public void SetArguments(Arguments arguments) => Arguments = arguments;
}
