﻿namespace Undersoft.SDK.Ethernet.Client
{
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using Undersoft.SDK.Ethernet.Transfer;
    using Undersoft.SDK.Invoking;

    public sealed class EthernetClient : IEthernetClient
    {
        private readonly ManualResetEvent connectNotice = new ManualResetEvent(false);
        public IPEndPoint EndPoint;
        private IPAddress ip;
        private int port;
        private Socket socket;
        private int timeout = 50;

        public EthernetClient(IPEndPoint endPoint)
        {
            ip = endPoint.Address;
            port = endPoint.Port;
            EndPoint = endPoint;
        }

        public IInvoker Connected { get; set; }

        public IInvoker HeaderReceived { get; set; }

        public IInvoker HeaderSent { get; set; }      

        public IInvoker MessageReceived { get; set; }

        public IInvoker MessageSent { get; set; }

        public ITransferContext Connect()
        {
            int _port = port;
            IPAddress _ip = ip;
            IPEndPoint endpoint = new IPEndPoint(_ip, _port);

            try
            {
                socket = new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Stream,
                    ProtocolType.Tcp
                );
                var context = new TransferContext(socket);
                socket.BeginConnect(endpoint, OnConnectCallback, context);
                connectNotice.WaitOne();

                Connected.Invoke(context);

                return context;
            }
            catch (SocketException ex) { throw new Exception("see inner exception", ex); }
        }

        public void Dispose()
        {
            connectNotice.Dispose();
            Close();
        }

        public bool IsConnected()
        {
            if (socket != null && socket.Connected)
                return !(
                    socket.Poll(timeout * 1000, SelectMode.SelectRead) && socket.Available == 0
                );
            return true;
        }

        public void Receive(TransferPart messagePart, ITransferContext context)
        {
            AsyncCallback callback = HeaderReceivedCallBack;
            if (messagePart != TransferPart.Header && context.HasMessageToReceive)
            {
                callback = MessageReceivedCallBack;
                context.ItemsLeft = context.Transfer.RequestHeader.Context.ItemsCount;
                context.Listener.BeginReceive(
                    context.MessageBuffer,
                    0,
                    context.BufferSize,
                    SocketFlags.None,
                    callback,
                    context
                );
            }
            else
                context.Listener.BeginReceive(
                    context.HeaderBuffer,
                    0,
                    context.BufferSize,
                    SocketFlags.None,
                    callback,
                    context
                );
        }

        public void Send(TransferPart messagePart, ITransferContext context)
        {
            if (!IsConnected())
                throw new Exception("Destination socket is not connected.");
            AsyncCallback callback = HeaderSentCallback;
            if (messagePart == TransferPart.Header)
            {
                callback = HeaderSentCallback;
                TransferOperation request = new TransferOperation(
                    context.Transfer,
                    TransferPart.Header,
                    DirectionType.Send
                );
                request.Resolve();
            }
            else if (context.HasMessageToSend)
            {
                callback = MessageSentCallback;
                context.OutputId = 0;
                TransferOperation request = new TransferOperation(
                    context.Transfer,
                    TransferPart.Message,
                    DirectionType.Send
                );
                request.Resolve();
            }
            else
                return;

            context.Listener.BeginSend(
                context.Output,
                0,
                context.Output.Length,
                SocketFlags.None,
                callback,
                context
            );
        }

        private void Close()
        {
            try
            {
                if (socket != null && socket.Connected)
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }                
            }
            catch (SocketException) { }
        }

        private void HeaderReceivedCallBack(IAsyncResult result)
        {
            ITransferContext context = (ITransferContext)result.AsyncState;
            int receive = context.Listener.EndReceive(result);

            if (receive > 0)
                context.ReadHeader(receive);

            if (context.Size > 0)
            {
                int buffersize =
                    (context.Size < context.BufferSize)
                        ? (int)context.Size
                        : context.BufferSize;
                context.Listener.BeginReceive(
                    context.HeaderBuffer,
                    0,
                    buffersize,
                    SocketFlags.None,
                    HeaderReceivedCallBack,
                    context
                );
            }
            else
            {
                TransferOperation request = new TransferOperation(
                    context.Transfer,
                    TransferPart.Header,
                    DirectionType.Receive
                );
                request.Resolve(context);

                if (!context.HasMessageToReceive && !context.HasMessageToSend)
                    context.Close = true;

                context.HeaderReceivedNotice.Set();
                HeaderReceived.Invoke(this);
            }
        }

        private void HeaderSentCallback(IAsyncResult result)
        {
            ITransferContext context = (ITransferContext)result.AsyncState;
            try
            {
                int sendcount = context.Listener.EndSend(result);
            }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }

            context.HeaderSentNotice.Set();
            HeaderSent.Invoke(this);
        }

        private void MessageReceivedCallBack(IAsyncResult result)
        {
            ITransferContext context = (ITransferContext)result.AsyncState;
            MarkupKind noiseKind = MarkupKind.None;

            int receive = context.Listener.EndReceive(result);

            if (receive > 0)
                noiseKind = context.ReadMessage(receive);

            if (context.Size > 0)
            {
                int buffersize =
                    (context.Size < context.BufferSize)
                        ? (int)context.Size
                        : context.BufferSize;
                context.Listener.BeginReceive(
                    context.MessageBuffer,
                    0,
                    buffersize,
                    SocketFlags.None,
                    MessageReceivedCallBack,
                    context
                );
            }
            else
            {
                object readPosition = context.InputId;

                if (
                    noiseKind == MarkupKind.Block
                    || (
                        noiseKind == MarkupKind.End
                        && (int)readPosition
                            < (context.Transfer.RequestHeader.Context.ItemsCount - 1)
                    )
                )
                    context.Listener.BeginReceive(
                        context.MessageBuffer,
                        0,
                        context.BufferSize,
                        SocketFlags.None,
                        MessageReceivedCallBack,
                        context
                    );

                TransferOperation request = new TransferOperation(
                    context.Transfer,
                    TransferPart.Message,
                    DirectionType.Receive
                );
                request.Resolve(context);

                if (
                    context.ItemsLeft <= 0
                    && !context.ChunksReceivedNotice.SafeWaitHandle.IsClosed
                )
                    context.ChunksReceivedNotice.Set();

                if (
                    noiseKind == MarkupKind.End
                    && (int)readPosition
                        >= (context.Transfer.RequestHeader.Context.ItemsCount - 1)
                )
                {
                    context.ChunksReceivedNotice.WaitOne();

                    if (context.HasMessageToSend)
                        context.MessageSentNotice.WaitOne();

                    context.Close = true;

                    context.MessageReceivedNotice.Set();
                    MessageReceived.Invoke(this);
                }
            }
        }

        private void MessageSentCallback(IAsyncResult result)
        {
            ITransferContext context = (ITransferContext)result.AsyncState;
            try
            {
                int sendcount = context.Listener.EndSend(result);
            }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }

            if (context.OutputId >= 0)
            {
                TransferOperation request = new TransferOperation(
                    context.Transfer,
                    TransferPart.Message,
                    DirectionType.Send
                );
                request.Resolve();
                context.Listener.BeginSend(
                    context.Output,
                    0,
                    context.Output.Length,
                    SocketFlags.None,
                    MessageSentCallback,
                    context
                );
            }
            else
            {
                if (!context.HasMessageToReceive)
                    context.Close = true;

                context.MessageSentNotice.Set();
                MessageSent.Invoke(this);
            }
        }

        private void OnConnectCallback(IAsyncResult result)
        {
            ITransferContext context = (ITransferContext)result.AsyncState;

            try
            {
                context.Listener.EndConnect(result);
                connectNotice.Set();
            }
            catch (SocketException ex) 
            {
                throw new Exception("see inner exception", ex);
            }
        }
    }
}
