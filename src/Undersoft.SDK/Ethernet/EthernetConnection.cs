namespace Undersoft.SDK.Ethernet.Connection
{
    using System.Net;
    using System.Threading;
    using Undersoft.SDK.Ethernet.Client;
    using Undersoft.SDK.Ethernet.Transfer;
    using Undersoft.SDK.Invoking;

    public interface IEthernetConnection
    {
        object Content { get; set; }

        void Close();

        ITransferContext Open(bool isAsync = true);

        void Reconnect();

        void SetCallback(IInvoker OnCompleteEvent);

        void SetCallback(string methodName, object classObject);
    }

    public class EthernetConnection : IEthernetConnection
    {
        private readonly ManualResetEvent completeNotice = new ManualResetEvent(false);
        public IInvoker CompleteMethod;
        private IInvoker connected;
        private IInvoker headerReceived;
        private IInvoker headerSent;
        private IInvoker messageReceived;
        private IInvoker messageSent;
        private bool isAsync = true;

        public EthernetConnection(
            EthernetClient client,
            IInvoker OnCompleteEvent = null,
            IInvoker OnEchoEvent = null
        )
        {
            Client = client;
            Transfer = new EthernetTransfer();

            connected = new EthernetMethod(nameof(this.Connected), this);
            headerSent = new EthernetMethod(nameof(this.HeaderSent), this);
            messageSent = new EthernetMethod(nameof(this.MessageSent), this);
            headerReceived = new EthernetMethod(nameof(this.HeaderReceived), this);
            messageReceived = new EthernetMethod(nameof(this.MessageReceived), this);

            client.Connected = connected;
            client.HeaderSent = headerSent;
            client.MessageSent = messageSent;
            client.HeaderReceived = headerReceived;
            client.MessageReceived = messageReceived;

            CompleteMethod = OnCompleteEvent;    

            WriteNotice("Client Connection Created");
        }

        public object Content
        {
            get { return Transfer.ResponseHeader.Data; }
            set { Transfer.ResponseHeader.Data = value; }
        }

        public EthernetTransfer Transfer { get; set; }

        private EthernetClient Client { get; set; }

        public void Close()
        {
            Client.Dispose();
        }

        public ITransferContext Connected(object contextState)
        {
            WriteNotice("Client Connection Established");
            Transfer.ResponseHeader.Context.Notice = "Client say Hello. ";

            ITransferContext context = (ITransferContext)contextState;
            context.Transfer = Transfer;

            Client.Send(TransferPart.Header, context);

            return context;
        }

        public ITransferContext HeaderReceived(object contextState)
        {
            string serverEcho = Transfer.RequestHeader.Context.Notice;
            WriteNotice(string.Format("Server header received"));
            if (serverEcho != null && serverEcho != "")
                WriteNotice(string.Format("Server echo: {0}", serverEcho));

            ITransferContext context = (ITransferContext)contextState;

            if (context.Close)
                context.Dispose();
            else
            {
                if (!context.Synchronic)
                {
                    if (context.HasMessageToSend)
                        Client.Send(TransferPart.Message, context);
                }

                if (context.HasMessageToReceive)
                    Client.Receive(TransferPart.Message, context);
            }

            if (!context.HasMessageToReceive && !context.HasMessageToSend)
            {
                if (CompleteMethod != null)
                    CompleteMethod.Invoke(context);
                if (!isAsync)
                    completeNotice.Set();
            }

            return context;
        }

        public ITransferContext HeaderSent(object contextState)
        {
            WriteNotice("Client header sent");
            ITransferContext context = (ITransferContext)contextState;
            if (!context.Synchronic)
                Client.Receive(TransferPart.Header, context);
            else
                Client.Send(TransferPart.Message, context);

            return context;
        }

        public ITransferContext Open(bool IsAsync = true)
        {
            isAsync = IsAsync;
            var context = Client.Connect();
            if (!IsAsync)
                completeNotice.WaitOne();

            return context;
        }

        public ITransferContext MessageReceived(object contextState)
        {
            WriteNotice(string.Format("Server message received"));

            ITransferContext context = (ITransferContext)contextState;
            if (context.Close)
                ((IEthernetClient)contextState).Dispose();

            if (CompleteMethod != null)
                CompleteMethod.Invoke(context);
            if (!isAsync)
                completeNotice.Set();
            return context;
        }

        public ITransferContext MessageSent(object contextState)
        {
            WriteNotice("Client message sent");

            ITransferContext context = (ITransferContext)contextState;
            if (context.Synchronic)
                Client.Receive(TransferPart.Header, context);

            if (!context.HasMessageToReceive)
            {
                if (CompleteMethod != null)
                    CompleteMethod.Invoke(context);
                if (!isAsync)
                    completeNotice.Set();
            }

            return context;
        }

        public void Reconnect()
        {
            IPEndPoint endpoint = new IPEndPoint(Client.EndPoint.Address, Client.EndPoint.Port);
            Transfer.Dispose();
            EthernetClient client = new EthernetClient(endpoint);
            Transfer = new EthernetTransfer(endpoint);
            client.Connected = connected;
            client.HeaderSent = headerSent;
            client.MessageSent = messageSent;
            client.HeaderReceived = headerReceived;
            client.MessageReceived = messageReceived;
            Client = client;
        }

        public void SetCallback(IInvoker OnCompleteEvent)
        {
            CompleteMethod = OnCompleteEvent;
        }

        public void SetCallback(string methodName, object classObject)
        {
            CompleteMethod = new EthernetMethod(methodName, classObject);
        }

        private void WriteNotice(string message)
        {
        }
    }
}
