using System.Net;
using System.Threading;
using Undersoft.SDK.Ethernet;
using Undersoft.SDK.Ethernet.Transfer;
using Undersoft.SDK.Invoking;

namespace Undersoft.SDK.Ethernet.Server
{
    public class EthernetServer : IEthernetServer
    {
        private IEthernetListener server;

        private Thread handler;

        public void ClearClients()
        {
            WriteNotice("Client registry cleaned");
            if (server != null)
                server.ClearClients();
        }

        public void Close()
        {
            if (server != null)
            {
                WriteNotice("Server instance shutdown ");
                server.CloseListener();
                server = null;
            }
            else
            {
                WriteNotice("Server instance doesn't exist ");
            }
        }

        public ITransferContext HeaderReceived(object contextState)
        {
            string clientEcho = ((ITransferContext)contextState)
                .Transfer
                .RequestHeader
                .Context
                .Notice;
            WriteNotice(string.Format("Client header received"));
            if (clientEcho != null && clientEcho != "")
                WriteNotice(string.Format("Client echo: {0}", clientEcho));

            EthernetContext trctx = ((ITransferContext)contextState).Transfer.ResponseHeader.Context;
            if (trctx.Notice == null || trctx.Notice == "")
                trctx.Notice = "Server say Hello";
            if (!((ITransferContext)contextState).Synchronic)
                server.Send(TransferPart.Header, ((ITransferContext)contextState).Id);
            else
                server.Receive(TransferPart.Message, ((ITransferContext)contextState).Id);

            return (ITransferContext)contextState;
        }

        public ITransferContext HeaderSent(object contextState)
        {
            WriteNotice("Server header sent");

            ITransferContext context = (ITransferContext)contextState;
            if (context.Close)
            {
                context.Transfer.Dispose();
                server.CloseClient(context.Id);
            }
            else
            {
                if (!context.Synchronic)
                {
                    if (context.HasMessageToReceive)
                        server.Receive(TransferPart.Message, context.Id);
                }
                if (context.HasMessageToSend)
                    server.Send(TransferPart.Message, context.Id);
            }
            return context;
        }

        public bool IsActive()
        {
            if (server != null)
            {
                WriteNotice("Server Instance Is Active");
                return true;
            }
            else
            {
                WriteNotice("Server Instance Doesn't Exist");
                return false;
            }
        }

        public ITransferContext MessageReceived(object contextState)
        {
            WriteNotice(string.Format("Client message received"));
            if (((ITransferContext)contextState).Synchronic)
                server.Send(TransferPart.Header, ((ITransferContext)contextState).Id);
            return (ITransferContext)contextState;
        }

        public ITransferContext MessageSent(object contextState)
        {
            WriteNotice("Server message sent");
            ITransferContext result = (ITransferContext)contextState;
            if (result.Close)
            {
                result.Transfer.Dispose();
                server.CloseClient(result.Id);
            }
            return result;
        }

        public void Start(IPEndPoint endPoint, IInvoker echoMethod = null
        )
        {
            server = new EthernetListener(endPoint);

            server.HeaderSent = new EthernetMethod(nameof(this.HeaderSent), this);
            server.MessageSent = new EthernetMethod(nameof(this.MessageSent), this);
            server.HeaderReceived = new EthernetMethod(nameof(this.HeaderReceived), this);
            server.MessageReceived = new EthernetMethod(nameof(this.MessageReceived), this);
            server.WriteEcho = echoMethod;

            handler = new Thread(new ThreadStart(server.StartListening));
            handler.Start();

            WriteNotice("Server instance started");
        }

        public void Stop()
        {
            handler.Join();
        }
        public void WriteNotice(string message)
        {
            if (server != null)
                server.WriteNotice(message);
        }
    }
}
