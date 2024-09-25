namespace Undersoft.SDK.Workflows
{
    using System.Collections;
    using System.Threading;
    using Notes;
    using Series;

    public class Workspace : IWorkspace
    {
        private static readonly int WAIT_FOR_OUTPUT_TIMEOUT = 30000;

        private ManualResetEventSlim sendingAccess = new ManualResetEventSlim(true, 128);

        private void AcquireSending(Worker worker)
        {
            do
            {
                if (!sendingAccess.Wait(WAIT_FOR_OUTPUT_TIMEOUT))
                    throw new TimeoutException("Waiting for result timeout has been exceeded");
            } while (!worker.CanSetOutput());
            sendingAccess.Reset();
        }
        private void ReleaseSending()
        {
            sendingAccess.Set();
        }

        private object holder = new object();

        public WorkNotes Notes;
        public bool Ready;
        public WorkAspects Case;
        public WorkAspect Aspect;
        private Thread[] workers;
        private int WorkersCount => Aspect.WorkersCount;
        private Registry<Worker> Workers = new Registry<Worker>();

        public Workspace(WorkAspect aspect)
        {
            Aspect = aspect;
            Case = Aspect.Case;
            Notes = Case.Notes;
            Ready = false;
        }

        public void Close(bool SafeClose)
        {
            foreach (Thread worker in workers)
            {
                Run(null);

                if (SafeClose && worker.ThreadState == ThreadState.Running)
                    worker.Join();
            }
            Ready = false;
        }

        public WorkAspect Allocate(int workersCount = 0)
        {
            if (workersCount > 0)
                Aspect.WorkersCount = workersCount;

            workers = new Thread[WorkersCount];
            for (int i = 0; i < WorkersCount; i++)
            {
                workers[i] = new Thread(Activate);
                workers[i].IsBackground = true;
                workers[i].Priority = ThreadPriority.AboveNormal;
                workers[i].Start();
            }

            Ready = true;
            return Aspect;
        }

        public void Run(WorkItem work)
        {
            lock (holder)
            {
                if (work != null)
                    Workers.Enqueue(work.Worker.Clone());
                else
                    Workers.Enqueue(DateTime.Now.Ticks, null);

                Monitor.Pulse(holder);
            }
        }

        public void Reset(int workersCount = 0)
        {
            Close(true);
            Allocate(workersCount);
        }

        public void Activate()
        {
            for (; ; )
            {
                Worker worker = null;
                object input = null;

                lock (holder)
                {
                    while (!Workers.TryDequeue(out worker))
                        Monitor.Wait(holder);

                    if (worker != null)
                        input = worker.GetInput();
                    else
                        return;
                }

                object result =
                    (input is object[])
                        ? worker.Process.Invoke((object[])input)
                        : worker.Process.Invoke(input);

                AcquireSending(worker);

                Send(worker, result);
                
                ReleaseSending();
            }
        }

        private void Send(Worker worker, object result)
        {
            worker.SetOutput(result);

            if (result == null)
                return;

            Notes.Send(
                worker
                    .Evokers.Where(e => e.Condition == null || e.Condition(result))
                    .ForEach(e => new WorkNote(worker.Work, e.Recipient, e, null, result)
                    {
                        SenderBox = worker.Work.Box,
                    })
            );
        }
    }
}
