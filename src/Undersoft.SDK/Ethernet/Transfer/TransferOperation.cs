using System.Collections;

namespace Undersoft.SDK.Ethernet.Transfer
{
    public class TransferOperation
    {
        private DirectionType direction;
        private ProtocolMethod method;
        private TransferPart part;
        private EthernetProtocol protocol;
        private EthernetSite site;
        private EthernetTransfer transit;
        private ITransferContext transitContext;
        private EthernetContext ethernetContext;

        public TransferOperation(
            EthernetTransfer _transaction,
            TransferPart _part,
            DirectionType _direction
        )
        {
            transit = _transaction;
            transitContext = transit.Context;
            ethernetContext = transit.ResponseHeader.Context;
            site = ethernetContext.Site;
            direction = _direction;
            part = _part;
            protocol = transitContext.Protocol;
            method = transitContext.Method;
        }

        public void Resolve(ITransferBuffer buffer = null)
        {
            switch (site)
            {
                case EthernetSite.Server:
                    switch (direction)
                    {
                        case DirectionType.Receive:
                            switch (part)
                            {
                                case TransferPart.Header:
                                    ServerReceivedTcpTransitHeader(buffer);
                                    break;
                                case TransferPart.Message:
                                    ServerReceivedTcpTransitMessage(buffer);
                                    break;
                            }
                            break;
                        case DirectionType.Send:
                            switch (part)
                            {
                                case TransferPart.Header:
                                    ServerSendTcpTransitHeader();
                                    break;
                                case TransferPart.Message:
                                    ServerSendTcpTransitMessage();
                                    break;
                            }
                            break;
                    }
                    break;
                case EthernetSite.Client:
                    switch (direction)
                    {
                        case DirectionType.Receive:
                            switch (part)
                            {
                                case TransferPart.Header:
                                    ClientReceivedTcpTransitHeader(buffer);
                                    break;
                                case TransferPart.Message:
                                    ClientReceivedTcpTransitMessage(buffer);
                                    break;
                            }
                            break;
                        case DirectionType.Send:
                            switch (part)
                            {
                                case TransferPart.Header:
                                    ClientSendTcpTransitHeader();
                                    break;
                                case TransferPart.Message:
                                    ClientSendTcpTrnsitMessage();
                                    break;
                            }
                            break;
                    }
                    break;
            }
        }

        private void ClientReceivedTcpTransitHeader(ITransferBuffer buffer)
        {
            TransferHeader headerObject = (TransferHeader)transit.ResponseHeader.Deserialize(buffer);

            if (headerObject != null)
            {
                transit.RequestHeader = headerObject;

                object reciveContent = transit.RequestHeader.Data;

                Type[] ifaces = reciveContent.GetType().GetInterfaces();
                if (
                    ifaces.Contains(typeof(ITransferable))
                    && ifaces.Contains(typeof(ITransferObject))
                )
                {
                    if (transit.ResponseHeader.Data == null)
                        transit.ResponseHeader.Data = ((ITransferObject)reciveContent).Locate();

                    object myContent = transit.ResponseHeader.Data;

                    ((ITransferObject)myContent).Merge(reciveContent);

                    int objectCount = transit.RequestHeader.Context.ItemsCount;
                    if (objectCount == 0)
                        transitContext.HasMessageToReceive = false;
                    else
                        transit.RequestMessage = new TransferMessage(
                            transit,
                            DirectionType.Receive,
                            myContent
                        );
                }
                else if (reciveContent is Hashtable)
                {
                    Hashtable hashTable = (Hashtable)reciveContent;
                    if (hashTable.Contains("Register"))
                    {
                        transitContext.Denied = !(bool)hashTable["Register"];
                        if (transitContext.Denied)
                        {
                            transitContext.Close = true;
                            transitContext.HasMessageToReceive = false;
                            transitContext.HasMessageToSend = false;
                        }
                    }
                }
                else
                    transitContext.HasMessageToSend = false;
            }
        }

        private void ClientReceivedTcpTransitMessage(ITransferBuffer buffer)
        {
            object serialItemsObj = ((object[])transit.RequestMessage.Data)[
                buffer.InputId
            ];
            ITransferable serialItems = (ITransferable)serialItemsObj;

            object deserialItemsObj = serialItems.Deserialize(buffer);
            ITransferable deserialItems = (ITransferable)deserialItemsObj;
            if (
                deserialItems.InputChunks <= deserialItems.CurrentChunk
                || deserialItems.CurrentChunk == 0
            )
            {
                transit.Context.ItemsLeft--;
                deserialItems.CurrentChunk = 0;
            }
        }

        private void ClientSendTcpTransitHeader()
        {            
            transit.Manager.GetHeaderData(out var data,                
                transitContext.Transfer.ResponseHeader.Data,
                DirectionType.Send
            );
            transitContext.Transfer.ResponseHeader.Data = data;
            if (transit.ResponseHeader.Context.ItemsCount == 0)
                transitContext.HasMessageToSend = false;

            transitContext.Transfer.ResponseHeader.Serialize(transitContext, 0, 0);
        }

        private void ClientSendTcpTrnsitMessage()
        {
            object serialitems = ((object[])transit.ResponseMessage.Data)[
                transitContext.ItemIndex
            ];

            int serialBlockId = ((ITransferable)serialitems).Serialize(
                transitContext,
                transitContext.OutputId,
                5000
            );
            if (serialBlockId < 0)
            {
                if (
                    transitContext.ItemIndex < transit.ResponseHeader.Context.ItemsCount - 1
                )
                {
                    transitContext.ItemIndex++;
                    transitContext.OutputId = 0;
                    return;
                }
            }
            transitContext.OutputId = serialBlockId;
        }

        private void ServerReceivedTcpTransitHeader(ITransferBuffer buffer)
        {
            bool isError = false;
            string errorMessage = "";
            try
            {
                TransferHeader headerObject = (TransferHeader)transit.ResponseHeader.Deserialize(buffer);
                if (headerObject != null)
                {
                    transit.RequestHeader = headerObject;


                    if (transit.RequestHeader.Context.Type != null)
                    {
                        object _content = transit.RequestHeader.Data;

                        Type[] ifaces = _content.GetType().GetInterfaces();
                        if (
                            ifaces.Contains(typeof(ITransferable))
                            && ifaces.Contains(typeof(ITransferObject))
                        )
                        {
                            int objectCount = transit.RequestHeader.Context.ItemsCount;
                            transitContext.Synchronic = transit
                                .RequestHeader
                                .Context
                                .Synchronic;

                            object myheader = ((ITransferObject)_content).Locate();

                            if (myheader != null)
                            {
                                if (objectCount == 0)
                                {
                                    transitContext.HasMessageToReceive = false;

                                    transit.ResponseHeader.Data = myheader;
                                }
                                else
                                {
                                    transit.ResponseHeader.Data = (
                                        (ITransferObject)myheader
                                    ).Merge(_content);
                                    transit.RequestMessage = new TransferMessage(
                                        transit,
                                        DirectionType.Receive,
                                        transit.ResponseHeader.Data
                                    );
                                }
                            }
                            else
                            {
                                isError = true;
                                errorMessage += "Prime not exist - incorrect object target ";
                            }
                        }
                        else
                        {
                            isError = true;
                            errorMessage += "Incorrect DPOT object - deserialization error ";
                        }
                    }
                    else
                    {
                        transit.ResponseHeader.Data = new Hashtable() { { "Register", true } };
                        transit.ResponseHeader.Context.Notice +=
                            "Registration success - Type: null ";
                    }
                }
                else
                {
                    isError = true;
                    errorMessage += "Incorrect DPOT object - deserialization error ";
                }
            }
            catch (Exception ex)
            {
                isError = true;
                errorMessage += ex.ToString();
            }

            if (isError)
            {
                transit.Context.Close = true;
                transit.Context.HasMessageToReceive = false;
                transit.Context.HasMessageToSend = false;

                if (errorMessage != "")
                {
                    transit.ResponseHeader.Data += errorMessage;
                    transit.ResponseHeader.Context.Notice += errorMessage;
                }
                transit.ResponseHeader.Context.Errors++;
            }
        }

        private void ServerReceivedTcpTransitMessage(ITransferBuffer buffer)
        {
            object serialItemsObj = ((object[])transit.RequestMessage.Data)[
                buffer.InputId
            ];
            object deserialItemsObj = ((ITransferable)serialItemsObj).Deserialize(buffer);
            ITransferable deserialItems = (ITransferable)deserialItemsObj;
            if (
                deserialItems.InputChunks <= deserialItems.CurrentChunk
                || deserialItems.CurrentChunk == 0
            )
            {
                transit.Context.ItemsLeft--;
                deserialItems.CurrentChunk = 0;
            }
        }

        private void ServerSendTcpTransitHeader()
        {
            transit.Manager.GetHeaderData(out var data,
                transitContext.Transfer.ResponseHeader.Data,
                DirectionType.Send
            );
            transitContext.Transfer.ResponseHeader.Data = data;

            if (transit.ResponseHeader.Context.ItemsCount == 0)
                transitContext.HasMessageToSend = false;

            transitContext.Transfer.ResponseHeader.Serialize(transitContext, 0, 0);
        }

        private void ServerSendTcpTransitMessage()
        {
            int serialBlockId = ((ITransferable[])transit.ResponseMessage.Data)[
                transitContext.ItemIndex
            ].Serialize(transitContext, transitContext.OutputId, 5000);

            if (serialBlockId < 0)
            {
                if (
                    transitContext.ItemIndex < transit.ResponseHeader.Context.ItemsCount - 1
                )
                {
                    transitContext.ItemIndex++;
                    transitContext.OutputId = 0;
                    return;
                }
            }
            transitContext.OutputId = serialBlockId;
        }
    }
}
