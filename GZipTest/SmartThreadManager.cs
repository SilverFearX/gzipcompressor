using System;
using System.Linq;
using System.Threading;

namespace GZipTest
{
    internal class SmartThreadManager<T> : IDisposable
    {
        private readonly Int32 ProcessorCount = Environment.ProcessorCount;

        public Boolean IsStarted { get; private set; }
        public Boolean IsStopped { get; private set; }
        public Boolean IsDisposed { get; private set; }
        public Boolean IsCompleted => SmartThreads.Sum(ST => ST.IsCompleted ? 1 : 0) == ProcessorCount;

        private SmartThread<T>[] SmartThreads;

        public SmartThreadManager()
        {
            SmartThreads = new SmartThread<T>[ProcessorCount];

            for (Int32 Index = 0; Index < ProcessorCount; Index++)
                SmartThreads[Index] = new SmartThread<T>();

            IsStarted = false;
            IsStopped = false;
            IsDisposed = false;
        }

        private static Int32 TaskSorter = 0;

        public void Enqueue(Task<T> Task)
        {
            if (IsStarted || IsStopped || IsDisposed) return;

            SmartThreads[TaskSorter++ % ProcessorCount].Enqueue(Task);
        }

        public void Start()
        {
            if (IsStarted || IsStopped || IsDisposed) return;

            for (Int32 Index = 0; Index < ProcessorCount; Index++)
                SmartThreads[Index].Start();

            IsStarted = true;
        }

        public void Wait()
        {
            for (Int32 Index = 0; Index < ProcessorCount; Index++)
                SmartThreads[Index].Wait();
        }

        public void Stop()
        {
            if (!IsStarted || IsStopped || IsDisposed) return;

            for (Int32 Index = 0; Index < ProcessorCount; Index++)
                SmartThreads[Index].Stop();

            IsStopped = true;
        }

        public void Dispose()
        {
            for (Int32 Index = 0; Index < ProcessorCount; Index++)
                SmartThreads[Index].Dispose();

            SmartThreads = null;

            IsDisposed = true;
        }
    }
}
