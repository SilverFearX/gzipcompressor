using System;

namespace GZipTest
{
    internal class Task<T>
    {
        private T Data;

        private Action<T> Action;

        public Task(T Data, Action<T> Action)
        {
            this.Data = Data;
            this.Action = Action;
        }

        public void Run()
        {
            Action(Data);
        }
    }
}
