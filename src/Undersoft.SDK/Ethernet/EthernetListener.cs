﻿namespace Undersoft.SDK.Ethernet
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using Undersoft.SDK.Ethernet.Transfer;
    using Undersoft.SDK.Invoking;
    using Undersoft.SDK.Series;
    using Undersoft.SDK.Uniques;

    public sealed class EthernetListener : IEthernetListener
    {
        private readonly Registry<ITransferContext> clients = new Registry<ITransferContext>();
        private readonly ManualResetEvent connectingNotice = new ManualResetEvent(false);
        private bool shutdown = false;
        private int timeout = 50;
        public int Limit { get; set; }
        public EndPoint EndPoint { get; set; }

        public EthernetListener(int limit = 10) { }

        public EthernetListener(IPEndPoint endPoint, int limit = 10) : this(limit) { EndPoint = endPoint; }

        public IInvoker HeaderReceived { get; set; }

        public IInvoker HeaderSent { get; set; }      

        public IInvoker MessageReceived { get; set; }

        public IInvoker MessageSent { get; set; }

        public IInvoker WriteEcho { get; set; }

        public void ClearClients()
        {
            foreach (ITransferContext closeContext in clients.AsValues())
            {
                ITransferContext context = closeContext;

                if (context == null)
                {
                    throw new Exception("Client does not exist.");
                }

                try
                {
                    context.Listener.Shutdown(SocketShutdown.Both);
                    context.Listener.Close();
                }
                catch (SocketException sx)
                {
                    WriteNotice(sx.Message);
                }
                finally
                {
                    context.Dispose();
                    WriteNotice(string.Format("Client disconnected with Id {0}", context.Id));
                }
            }
            clients.Clear();
        }

        public void CloseClient(ISeriesItem<ITransferContext> item)
        {
            ITransferContext context = item.Value;

            if (context == null)
            {
                WriteNotice(string.Format("Client {0} does not exist.", context.Id));
            }
            else
            {
                try
                {
                    if (context.Listener != null && context.Listener.Connected)
                    {
                        context.Listener.Shutdown(SocketShutdown.Both);
                        context.Listener.Close();
                    }
                }
                catch (SocketException sx)
                {
                    WriteNotice(sx.Message);
                }
                finally
                {
                    ITransferContext contextRemoved = clients.Remove(context.Id);
                    contextRemoved.Dispose();
                    WriteNotice(string.Format("Client disconnected with Id {0}", context.Id));
                }
            }
        }

        public void CloseClient(int id)
        {
            CloseClient(GetClient(id));
        }

        public void CloseListener()
        {
            foreach (ITransferContext closeContext in clients.AsValues())
            {
                ITransferContext context = closeContext;

                if (context == null)
                {
                    WriteNotice(string.Format("Client  does not exist."));
                }
                else
                {
                    try
                    {
                        if (context.Listener != null && context.Listener.Connected)
                        {
                            context.Listener.Shutdown(SocketShutdown.Both);
                            context.Listener.Close();
                        }
                    }
                    catch (SocketException sx)
                    {
                        WriteNotice(sx.Message);
                    }
                    finally
                    {
                        context.Dispose();
                        WriteNotice(string.Format("Client disconnected with Id {0}", context.Id));
                    }
                }
            }
            clients.Clear();
            shutdown = true;
            connectingNotice.Set();
            GC.Collect();
        }

        public void EthHeaderReceived(ITransferContext context)
        {
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
                    HeaderReceivedCallback,
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

                context.HeaderReceivedNotice.Set();

                try
                {
                    HeaderReceived.Invoke(context);
                }
                catch (Exception ex)
                {
                    WriteNotice(ex.Message);
                    CloseClient(context.Id);
                }
            }
        }

        public void Dispose()
        {
            foreach (var item in clients.AsItems())
            {
                CloseClient(item);
            }

            connectingNotice.Dispose();
        }

        public void WriteNotice(string message)
        {
            if (WriteEcho != null)
                WriteEcho.Invoke(message);
        }

        public void HeaderReceivedCallback(IAsyncResult result)
        {
            ITransferContext context = (ITransferContext)result.AsyncState;
            int receive = context.Listener.EndReceive(result);

            if (receive > 0)
                context.ReadHeader(receive);

            if (context.Protocol == EthernetProtocol.DOTP)
                EthHeaderReceived(context);
            else if (context.Protocol == EthernetProtocol.HTTP)
                HttpHeaderReceived(context);
        }

        public void HeaderSentCallback(IAsyncResult result)
        {
            ITransferContext context = (ITransferContext)result.AsyncState;
            try
            {
                int sendcount = context.Listener.EndSend(result);
            }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }

            if (!context.HasMessageToReceive && !context.HasMessageToSend)
            {
                context.Close = true;
            }

            context.HeaderSentNotice.Set();

            try
            {
                HeaderSent.Invoke(context);
            }
            catch (Exception ex)
            {
                WriteNotice(ex.Message);
                CloseClient(context.Id);
            }
        }

        public void HttpHeaderReceived(ITransferContext context)
        {
            if (context.Size > 0)
            {
                context.Listener.BeginReceive(
                    context.HeaderBuffer,
                    0,
                    context.BufferSize,
                    SocketFlags.None,
                    HeaderReceivedCallback,
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

                context.HeaderReceivedNotice.Set();

                try
                {
                    HeaderReceived.Invoke(context);
                }
                catch (Exception ex)
                {
                    WriteNotice(ex.Message);
                    CloseClient(context.Id);
                }
            }
        }

        public bool IsConnected(int id)
        {
            ITransferContext context = GetClient(id).Value;
            if (context != null && context.Listener != null && context.Listener.Connected)
                return !(
                    context.Listener.Poll(timeout * 100, SelectMode.SelectRead)
                    && context.Listener.Available == 0
                );
            else
                return false;
        }

        public void MessageReceivedCallback(IAsyncResult result)
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
                    MessageReceivedCallback,
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
                        MessageReceivedCallback,
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
                    context.MessageReceivedNotice.Set();

                    try
                    {
                        MessageReceived.Invoke(context);
                    }
                    catch (Exception ex)
                    {
                        WriteNotice(ex.Message);
                        CloseClient(context.Id);
                    }
                }
            }
        }

        public void MessageSentCallback(IAsyncResult result)
        {
            ITransferContext context = (ITransferContext)result.AsyncState;
            try
            {
                int sendcount = context.Listener.EndSend(result);
            }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }

            if (
                context.OutputId >= 0
                || context.ItemIndex < (context.Transfer.ResponseHeader.Context.ItemsCount - 1)
            )
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
                if (context.HasMessageToReceive)
                    context.MessageReceivedNotice.WaitOne();

                context.Close = true;

                context.MessageSentNotice.Set();

                try
                {
                    MessageSent.Invoke(context);
                }
                catch (Exception ex)
                {
                    WriteNotice(ex.Message);
                    CloseClient(context.Id);
                }
            }
        }

        public void OnConnectCallback(IAsyncResult result)
        {
            try
            {
                if (!shutdown)
                {
                    ITransferContext context;
                    int id = -1;
                    id = (int)Unique.NewId.UniqueKey32();
                    context = new TransferContext(
                        ((Socket)result.AsyncState).EndAccept(result),
                        id
                    );
                    context.Transfer = new EthernetTransfer(null, context);
                    while (true)
                    {
                        if (!clients.Add(id, context))
                        {
                            id = (int)Unique.NewId.UniqueKey32();
                            context.Id = id;
                        }
                        else
                            break;
                    }
                    WriteNotice("Client connected. Get Id " + id);
                    context.Listener.BeginReceive(
                        context.HeaderBuffer,
                        0,
                        context.BufferSize,
                        SocketFlags.None,
                        HeaderReceivedCallback,
                        clients[id]
                    );
                }
                connectingNotice.Set();
            }
            catch (SocketException sx)
            {
                WriteNotice(sx.Message);
            }
        }

        public void Receive(TransferPart messagePart, int id)
        {
            ITransferContext context = GetClient(id).Value;

            AsyncCallback callback = HeaderReceivedCallback;

            if (messagePart != TransferPart.Header && context.HasMessageToReceive)
            {
                callback = MessageReceivedCallback;
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

        public void Send(TransferPart messagePart, int id)
        {
            ITransferContext context = GetClient(id).Value;
            if (!IsConnected(context.Id))
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

        public void StartListening()
        {           
            shutdown = false;
            try
            {
                using (
                    Socket socket = new Socket(
                        AddressFamily.InterNetwork,
                        SocketType.Stream,
                        ProtocolType.Tcp
                    )
                )
                {
                    socket.Bind(EndPoint);
                    socket.Listen(Limit);
                    while (!shutdown)
                    {
                        connectingNotice.Reset();
                        socket.BeginAccept(OnConnectCallback, socket);
                        connectingNotice.WaitOne();
                    }
                }
            }
            catch (SocketException sx)
            {
                WriteNotice(sx.Message);
            }
        }

        private ISeriesItem<ITransferContext> GetClient(int id)
        {
            return clients.GetItem(id);
        }
    }
}
