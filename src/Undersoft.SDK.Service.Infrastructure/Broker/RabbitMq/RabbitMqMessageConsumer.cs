﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Timers;


namespace Undersoft.SDK.Service.Data.Event.Provider.RabbitMq
{
    public class RabbitMqMessageConsumer : IRabbitMqMessageConsumer, IDisposable
    {
        public ILogger<RabbitMqMessageConsumer> Logger { get; set; }

        protected IConnectionPool ConnectionPool { get; }

        protected System.Timers.Timer Timer { get; }

        protected ExchangeDeclareConfiguration? Exchange { get; private set; }

        protected QueueDeclareConfiguration? Queue { get; private set; }

        protected string? ConnectionName { get; private set; }

        protected ConcurrentBag<Func<IModel?, BasicDeliverEventArgs, Task>> Callbacks { get; }

        protected IModel? Channel { get; private set; }

        protected ConcurrentQueue<QueueBindCommand> QueueBindCommands { get; }

        protected object ChannelSendSyncLock { get; } = new object();

        public RabbitMqMessageConsumer(
            IConnectionPool connectionPool)
        {
            ConnectionPool = connectionPool;
            Logger = NullLogger<RabbitMqMessageConsumer>.Instance;
            QueueBindCommands = new ConcurrentQueue<QueueBindCommand>();
            Callbacks = new ConcurrentBag<Func<IModel?, BasicDeliverEventArgs, Task>>();
            Timer = new System.Timers.Timer();
            Timer.Interval = 5000; //5 sec.
            Timer.Elapsed += Timer_Elapsed;
        }

        public void Initialize(
            [DisallowNull] ExchangeDeclareConfiguration exchange,
            [DisallowNull] QueueDeclareConfiguration queue,
            string? connectionName = null)
        {
            Exchange = exchange;
            Queue = queue;
            ConnectionName = connectionName;
            Timer.Start();
        }

        public virtual async Task BindAsync(string routingKey)
        {
            QueueBindCommands.Enqueue(new QueueBindCommand(QueueBindType.Bind, routingKey));
            await TrySendQueueBindCommandsAsync();
        }

        public virtual async Task UnbindAsync(string routingKey)
        {
            QueueBindCommands.Enqueue(new QueueBindCommand(QueueBindType.Unbind, routingKey));
            await TrySendQueueBindCommandsAsync();
        }

        protected virtual async Task TrySendQueueBindCommandsAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    while (!QueueBindCommands.IsEmpty)
                    {
                        if (Channel == null || Channel.IsClosed)
                        {
                            return;
                        }

                        lock (ChannelSendSyncLock)
                        {
                            if (QueueBindCommands.TryPeek(out var command))
                            {
                                switch (command.Type)
                                {
                                    case QueueBindType.Bind:
                                        Channel.QueueBind(
                                            queue: Queue?.QueueName,
                                            exchange: Exchange?.ExchangeName,
                                            routingKey: command.RoutingKey
                                        );
                                        break;
                                    case QueueBindType.Unbind:
                                        Channel.QueueUnbind(
                                            queue: Queue?.QueueName,
                                            exchange: Exchange?.ExchangeName,
                                            routingKey: command.RoutingKey
                                        );
                                        break;
                                    default:
                                        throw new Exception($"Unknown {nameof(QueueBindType)}: {command.Type}");
                                }

                                QueueBindCommands.TryDequeue(out command);
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    this.Warning<Netlog>("", null, ex);
                }
            }
             );
        }

        public virtual void OnMessageReceived(Func<IModel?, BasicDeliverEventArgs, Task> callback)
        {
            Callbacks.Add(callback);
        }

        protected virtual void Timer_Elapsed(object? sender, ElapsedEventArgs args)
        {
            if (Channel == null || Channel.IsOpen == false)
            {
                _ = TryCreateChannelAsync();
                _ = TrySendQueueBindCommandsAsync();
            }
        }

        protected virtual async Task TryCreateChannelAsync()
        {
            await Task.Run(() =>
            {
                DisposeChannel();

                try
                {
                    if (Exchange == null || ConnectionName == null || Queue == null || Channel == null)
                        throw new Exception("Exchange, ConnectionName, Queue, Channel cannot be null");

                    Channel = ConnectionPool
                        .Get(ConnectionName!)
                        .CreateModel();

                    Channel.ExchangeDeclare(
                        exchange: Exchange.ExchangeName,
                        type: Exchange.Type,
                        durable: Exchange.Durable,
                        autoDelete: Exchange.AutoDelete,
                        arguments: Exchange.Arguments
                    );

                    Channel.QueueDeclare(
                        queue: Queue.QueueName,
                        durable: Queue.Durable,
                        exclusive: Queue.Exclusive,
                        autoDelete: Queue.AutoDelete,
                        arguments: Queue.Arguments
                    );

                    if (Queue.PrefetchCount.HasValue)
                    {
                        Channel.BasicQos(0, Queue.PrefetchCount.Value, false);
                    }

                    var consumer = new AsyncEventingBasicConsumer(Channel);
                    consumer.Received += HandleIncomingMessageAsync;

                    Channel.BasicConsume(
                        queue: Queue.QueueName,
                        autoAck: false,
                        consumer: consumer
                    );
                }
                catch (Exception ex)
                {
                    this.Warning<Netlog>("", null, ex);
                }
            }
             );
        }

        protected virtual async Task HandleIncomingMessageAsync(object sender, BasicDeliverEventArgs basicDeliverEventArgs)
        {
            try
            {
                foreach (var callback in Callbacks)
                {
                    await callback(Channel, basicDeliverEventArgs);
                }

                Channel?.BasicAck(basicDeliverEventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                try
                {
                    Channel?.BasicNack(
                        basicDeliverEventArgs.DeliveryTag,
                        multiple: false,
                        requeue: true
                    );
                }
                catch { }

                this.Warning<Netlog>("", null, ex);
            }
        }


        protected virtual void DisposeChannel()
        {
            if (Channel == null)
            {
                return;
            }

            try
            {
                Channel.Dispose();
            }
            catch (Exception ex)
            {
                this.Warning<Netlog>("", null, ex);
            }
        }

        public virtual void Dispose()
        {
            Timer.Stop();
            DisposeChannel();
        }

        protected class QueueBindCommand
        {
            public QueueBindType Type { get; }

            public string RoutingKey { get; }

            public QueueBindCommand(QueueBindType type, string routingKey)
            {
                Type = type;
                RoutingKey = routingKey;
            }
        }

        protected enum QueueBindType
        {
            Bind,
            Unbind
        }
    }
}
