﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Undersoft.SDK.Ethernet.Transfer
{    
    public class TransferMessage : ITransferable, IDisposable
    {
        private object data;
        private DirectionType direction;
        
        private EthernetTransfer transaction;

        public TransferMessage()
        {
            data = new object();
            OutputChunks = 0;
            InputChunks = 0;
            direction = DirectionType.Receive;
        }

        public TransferMessage(
            EthernetTransfer _transaction,
            DirectionType _direction,
            object message = null
        )
        {
            transaction = _transaction;
            direction = _direction;

            if (message != null)
                Data = message;
            else
                data = new object();

            OutputChunks = 0;
            InputChunks = 0;
        }

        public object Data
        {
            get { return data; }
            set { transaction.Manager.GetMessageData(out data, value, direction); }
        }

        public int InputChunks { get; set; }

        public int ItemsCount
        {
            get { return data != null ? ((ITransferable[])data).Sum(t => t.ItemsCount) : 0; }
        }

        public string Notice { get; set; }

        public int ObjectsCount
        {
            get { return data != null ? ((ITransferable[])data).Length : 0; }
        }

        public int CurrentChunk { get; set; }

        public int OutputChunks { get; set; }

        public object Deserialize(ITransferBuffer buffer)
        {
            return -1;
        }

        public object Deserialize(Stream fromstream)
        {
            return -1;
        }

        public void Dispose()
        {
            data = null;
        }

        public object GetHeader()
        {
            if (direction == DirectionType.Send)
                return transaction.ResponseHeader.Data;
            else
                return transaction.RequestHeader.Data;
        }

        public object[] GetMessage()
        {
            if (data != null)
                return (ITransferable[])data;
            return null;
        }

        public int Serialize(ITransferBuffer buffer, int offset, int batchSize)
        {
            buffer = transaction.Context;
            buffer.Output = this.ToJsonBytes();
            return buffer.Output.Length;
        }

        public int Serialize(Stream tostream, int offset, int batchSize)
        {
            this.ToJsonStream(tostream);
            return (int)tostream.Length;
        }
    }
}
