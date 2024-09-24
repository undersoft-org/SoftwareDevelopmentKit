namespace Undersoft.SDK.Workflows
{
    using Notes;
    using Series;
    using Undersoft.SDK;
    using Undersoft.SDK.Invoking;
    using Uniques;

    public class Worker : Origin, IWorker
    {
        public Registry<object> Input = new Registry<object>(true);
        public Registry<object> Output  = new Registry<object>(true);

        public Worker RootWorker;

        private Worker() { }

        public Worker(string Name, IInvoker Method) : this()
        {
            Process = Method;
            this.Name = Name;
            TypeId = Process.Id;
            Id = Unique.NewId.UniqueKey(TypeId);
            RootWorker = this;
        }

        public IUnique Empty => new Uscn();

        public WorkNoteEvokers Evokers { get; set; } = new WorkNoteEvokers();

        public object GetInput()
        {
            if(Input.TryGet(WorkerOrder, out object entry))
                Input.Remove(WorkerOrder);
            return entry;
        }

        public void SetInput(object value)
        {
            Input.Enqueue(Interlocked.Increment(ref RootWorker.InputOrder), value);
        }

        public object GetOutput()
        {
            if (Output.TryGet(WorkerOrder, out object entry))
                Output.Remove(WorkerOrder);
            return entry;
        }

        public void SetOutput(object value)
        {
            Output.Enqueue(Interlocked.Increment(ref RootWorker.OutputOrder), value);            
        }

        public void WaitForOutput()
        {
            SpinWait.SpinUntil(() => WorkerOrder - 1 == RootWorker.OutputOrder);            
        }

        public Worker Clone()
        {
            var _worker = new Worker(Name, Process);
            _worker.WorkerOrder = Interlocked.Increment(ref RootWorker.WorkerOrder);
            _worker.RootWorker = this;
            _worker.Input = Input;
            _worker.Output = Output;
            _worker.Evokers = Evokers;
            _worker.Work = Work;
            return _worker;
        }

        public WorkItem Work { get; set; }

        public string Name { get; set; }

        public IInvoker Process { get; set; }

        public int OutputOrder;

        public int InputOrder;

        public int WorkerOrder;

        public WorkAspect FlowTo<T>(string methodName = null)
        {
            return Work.FlowTo<T>(methodName);
        }

        public WorkAspect FlowTo<T>(Func<T, Delegate> methodName) where T : class, new()
        {
            return Work.FlowTo(methodName);
        }

        public WorkAspect FlowTo(WorkItem recipient)
        {
            Evokers.Add(new WorkNoteEvoker(Work, recipient, Work));
            return Work.Aspect;
        }

        public WorkAspect FlowTo(WorkItem Recipient, params WorkItem[] RelationWorks)
        {
            Evokers.Add(new WorkNoteEvoker(Work, Recipient, RelationWorks));
            return Work.Aspect;
        }

        public WorkAspect FlowTo(string RecipientName, string methodName)
        {
            Evokers.Add(new WorkNoteEvoker(Work, RecipientName, Name));
            return Work.Aspect;
        }

        public WorkAspect FlowTo(string RecipientName, params string[] RelationNames)
        {
            Evokers.Add(new WorkNoteEvoker(Work, RecipientName, RelationNames));
            return Work.Aspect;
        }

        public WorkAspect FlowFrom<T>(string methodName = null)
        {
            return Work.FlowFrom<T>(methodName);
        }

        public WorkAspect FlowFrom<T>(Func<T, Delegate> methodName) where T : class, new()
        {
            return Work.FlowFrom(methodName);
        }

        public WorkAspect FlowFrom(WorkItem sender)
        {
            Work.FlowFrom(sender);
            return Work.Aspect;
        }

        public WorkAspect FlowFrom(WorkItem Sender, params WorkItem[] RelationWorks)
        {
            Work.FlowFrom(Sender, RelationWorks);
            return Work.Aspect;
        }

        public WorkAspect FlowFrom(string SenderName, string methodName)
        {
            Work.FlowFrom(SenderName, methodName);
            return Work.Aspect;
        }

        public WorkAspect FlowFrom(string SenderName, params string[] RelationNames)
        {
            Work.FlowFrom(SenderName, RelationNames);
            return Work.Aspect;
        }
    }
}
