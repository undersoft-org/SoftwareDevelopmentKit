namespace Undersoft.SDK.Workflows
{
    using System.Collections;
    using System.Threading;
    using Notes;
    using Series;

    public class Workspace : IWorkspace
    {
        private static readonly int WAIT_WRITE_TIMEOUT = 5000;

        private ManualResetEventSlim postAccess = new ManualResetEventSlim(true, 128);
        private SemaphoreSlim postPass = new SemaphoreSlim(1);
        private object inputHolder = new object();
        private object outputHolder = new object();

        private void acquirePostAccess()
        {
            do
            {
                if (!postAccess.Wait(WAIT_WRITE_TIMEOUT))
                    continue;
                postAccess.Reset();
            } while (!postPass.Wait(0));
        }

        private void releasePostAccess()
        {
            postPass.Release();
            postAccess.Set();
        }

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
            lock (inputHolder)
            {
                if (work != null)
                    Workers.Enqueue(work.Worker.Clone());
                else
                    Workers.Enqueue(DateTime.Now.Ticks, null);

                Monitor.Pulse(inputHolder);
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

                lock (inputHolder)
                {
                    while (!Workers.TryDequeue(out worker))
                        Monitor.Wait(inputHolder);

                    if (worker != null)
                        input = worker.GetInput();
                    else
                        return;
                }

                object output =
                    (input is object[])
                        ? worker.Process.Invoke((object[])input)
                        : worker.Process.Invoke(input);

                worker.WaitForOutput();

                lock (outputHolder)
                {
                    Outpost(worker, output);
                }
            }
        }

        private void Outpost(Worker worker, object output)
        {
            worker.SetOutput(output);

            if (output != null)
            {
                if (worker.Evokers != null && worker.Evokers.Count > 0)
                {
                    int l = worker.Evokers.Count;
                    if (l > 0)
                    {
                        var notes = new WorkNote[l];
                        for (int i = 0; i < worker.Evokers.Count; i++)
                        {
                            WorkNote note = new WorkNote(
                                worker.Work,
                                worker.Evokers[i].Recipient,
                                worker.Evokers[i],
                                null,
                                output
                            );
                            note.SenderBox = worker.Work.Box;
                            notes[i] = note;
                        }

                        Notes.Send(notes);
                    }
                }
            }
        }
    }
}
