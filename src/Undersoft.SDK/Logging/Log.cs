namespace Undersoft.SDK.Logging
{
    using System.Collections.Concurrent;
    using System.Linq.Expressions;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using Serilog.Events;

    public static partial class Log
    {
        private static readonly int BACK_LOG_DAYS = -1;
        private static readonly int BACK_LOG_HOURS = -1;
        private static readonly int BACK_LOG_MINUTES = -1;
        private static readonly int SYNC_CLOCK_INTERVAL = 15;
        private static readonly JsonSerializerOptions jsonOptions;
        private static Task logging;
        private static CancellationTokenSource cancellation = new CancellationTokenSource();
        private static int level = 2;
        private static bool clearable = false;
        private static DateTime expiration;
        private static ILogHandler handler { get; set; }

        private static ConcurrentQueue<Starlog> queue = new ConcurrentQueue<Starlog>();

        private static bool active;
        
        public static DateTime Clock = DateTime.Now;

        static Log()
        {
            jsonOptions = JsonOptionsBuilder();
            handler = new LogHandler(jsonOptions, LogEventLevel.Information);
            expiration = DateTime
                .Now.AddDays(BACK_LOG_DAYS)
                .AddHours(BACK_LOG_HOURS)
                .AddMinutes(BACK_LOG_MINUTES);
            Start(level);
        }

        public static void Add(
            LogEventLevel logLevel,
            string category,
            string message,
            ILogSate state
        )
        {
            var log = new Starlog()
            {
                Level = logLevel,
                Sender = category,
                State = state,
                Message = message,
            };

            queue.Enqueue(log);
        }

        public static void Clear()
        {
            if (!clearable || handler == null)
                return;

            try
            {
                if (DateTime.Now.Day != expiration.Day)
                {
                    if (DateTime.Now.Hour != expiration.Hour)
                    {
                        if (DateTime.Now.Minute != expiration.Minute)
                        {
                            handler.Clean(expiration);
                            expiration = DateTime.Now;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("see inner exception", ex);
            }
        }

        public static void CreateHandler(LogEventLevel level)
        {
            handler = new LogHandler(jsonOptions, level);
        }

        public static void SetLevel(int logLevel)
        {
            level = logLevel;
        }

        public static void Start(int logLevel)
        {
            CreateHandler(LogEventLevel.Information);
            SetLevel(logLevel);
            if (!active)
            {
                active = true;
                logging = Task.Factory.StartNew(Logging, cancellation.Token);
            }
        }

        private static async Task Logging()
        {
            try
            {
                int syncInterval = SYNC_CLOCK_INTERVAL;
                while (active)
                {
                    if (--syncInterval > 0)
                        Clock = Clock.AddMilliseconds(1005);
                    else
                    {
                        Clock = DateTime.UtcNow;
                        syncInterval = SYNC_CLOCK_INTERVAL;
                    }
                    await Task.Delay(1000).ConfigureAwait(false);
                    if (handler != null)
                    {
                        int count = queue.Count;
                        for (int i = 0; i < count; i++)
                        {
                            if (queue.TryDequeue(out Starlog log))
                            {
                                handler.Write(log);
                            }
                        }
                    }

                    if (clearable)
                        Clear();
                }
            }
            catch (Exception ex)
            {
                Stop();
                throw new Exception("see inner exception", ex);
            }
        }

        private static void Stop()
        {
            cancellation.Cancel();
            active = false;
        }

        private static JsonSerializerOptions JsonOptionsBuilder()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new LogExceptionConverter());
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            return options;
        }
    }
}
