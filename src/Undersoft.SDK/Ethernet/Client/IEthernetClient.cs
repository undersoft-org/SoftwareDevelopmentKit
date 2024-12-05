using Undersoft.SDK.Ethernet.Transfer;
using Undersoft.SDK.Invoking;

namespace Undersoft.SDK.Ethernet.Client
{
    public interface IEthernetClient : IDisposable
    {
        IInvoker Connected { get; set; }    

        IInvoker HeaderReceived { get; set; }

        IInvoker HeaderSent { get; set; }

        IInvoker MessageReceived { get; set; }

        IInvoker MessageSent { get; set; }

        ITransferContext Connect();

        bool IsConnected();

        void Receive(TransferPart messagePart, ITransferContext context);

        void Send(TransferPart messagePart, ITransferContext context);
    }
}
