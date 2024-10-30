using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Undersoft.SDK.Service.Infrastructure.Telemetry
{    
    public class OperationTelemetry : Origin, IDisposable
    {       
        private ActivityListener activityListener { get; }

        public OperationTelemetry()
        {
            string? version = typeof(OperationTelemetry).Assembly.GetName().Version?.ToString();

            this.ActivitySource = new ActivitySource("Undersoft.SDK.Service.Operation", version);
            
            activityListener = new ActivityListener();            
            activityListener.ActivityStarted = a => ActivityStarted(a);
            activityListener.ActivityStopped = a => ActivityStopped(a);            
            ActivitySource.AddActivityListener(activityListener);

            this.Meter = new Meter("Undersoft.SDK.Service.Operation", version);            
            this.Counter = Meter.CreateCounter<long>("operation_counter", description: "The number of operations");
            this.Duration = Meter.CreateHistogram<double>("operation_duration", description: "The time of operations");
            this.Active = Meter.CreateUpDownCounter<long>("operation_active", description: "The number of active operations");           
        }

        public Meter Meter { get; }

        public ActivitySource ActivitySource { get; }

        public Counter<long> Counter { get; }        

        public Histogram<double> Duration { get; }

        public UpDownCounter<long> Active { get; }

        public Activity StartActivity(IOperation request)
        {
            request.Info<Apilog>($"Operation request input", request.Input);
         
            var activity = ActivitySource.StartActivity(
                $"{request.GetType().BaseType?.Name.FirstDelimited('`')} Operation",
                Enum.Parse<ActivityKind>(request.Site.ToString())
            );

            return activity!;
        }

        public void AddTags(Activity? activity, IOperation request, IOperation response)
        {           
            Task.Factory.StartNew(() =>
            {                
                var type = request.GetType();
                var genericTypes = type.GetGenericArguments();

                activity!.AddTag("type", type.Name.FirstDelimited('`'));                
                activity!.AddTag("kind", request.Kind.ToString());                
                activity!.AddTag("store", genericTypes[0].Name.Substring(1));
                activity!.AddTag("valid", response.Validation.IsValid.ToString());
                if (request.Site == OperationSite.Client)
                {
                    activity!.AddTag("contract", genericTypes[1].FullName);
                    activity!.AddTag("model", genericTypes[2].FullName);
                }
                else
                {
                    activity!.AddTag("entity", genericTypes[1].FullName);
                    activity!.AddTag("contract", genericTypes[2].FullName);
                }
            });

            response.Info<Apilog>($"Operation response output", response.Output);
        }

        public void ActivityStarted(Activity activity)
        {
            Counter.Add(1);
            Active.Add(1);
        }

        public void ActivityStopped(Activity activity)
        {
            Duration.Record(activity.Duration.TotalMilliseconds);
            Active.Add(-1);
        }

        public void Dispose()
        {
            this.ActivitySource.Dispose();
            this.activityListener.Dispose();
            this.Meter.Dispose();
        }
    }



    public struct Interval
    {
        public long Start;
        public long End;        

        public Interval(long start, long end)
        {
            this.Start = start;
            this.End = end;
        }
    }
}