using System;
using System.Collections.Generic;
using System.Threading;

namespace GZipTest
{
    internal class SmartThread<T> : IDisposable
    {
        private static Int32 SmartThreadCount = 0;

        private Thread Thread;

        private AutoResetEvent CompletedEvent;

        public Boolean IsStarted { get; private set; }
        public Boolean IsStopped { get; private set; }
        public Boolean IsDisposed { get; private set; }
        public Boolean IsCompleted { get; private set; }

        public Queue<Task<T>> TaskQueue { get; private set; }

        public SmartThread()
        {
            Thread = new Thread(Loop);
            Thread.Name = $"SmartThread #{SmartThreadCount++}";

            CompletedEvent = new AutoResetEvent(false);

            IsStarted = false;
            IsStopped = false;
            IsDisposed = false;
            IsCompleted = false;

            TaskQueue = new Queue<Task<T>>();
        }

        public void Enqueue(Task<T> Task)
        {
            if (IsStarted || IsStopped || IsDisposed) return;

            TaskQueue.Enqueue(Task);
        }

        public void Start()
        {
            if (IsStarted) return;

            Thread.Start();

            IsStarted = true;
        }

        public void Wait()
        {
            if (IsCompleted || IsDisposed) return;

            CompletedEvent.WaitOne();
        }

        public void Stop()
        {
            if (!IsStarted) return;

            IsStopped = true;
        }

        public void Dispose()
        {
            Thread.Join();

            Thread = null;

            IsDisposed = true;
        }

        // основной цикл потока
        private void Loop()
        {
            while (!IsStopped)
            {
                Task<T> Task = null;

                lock (TaskQueue)
                {
                    if (TaskQueue.Count == 0) break;

                    Task = TaskQueue.Dequeue();
                }

                Task.Run();
            }

            IsCompleted = true;

            CompletedEvent.Set();
        }
    }
}
