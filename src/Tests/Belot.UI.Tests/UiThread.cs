namespace Belot.UI.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Runtime.ExceptionServices;
    using System.Threading;
    using System.Threading.Tasks;

    // Runs a test the way the app runs a game: on one thread with a SynchronizationContext, so
    // every continuation of the session (and so every event it raises) comes back to that thread.
    internal static class UiThread
    {
        public static int Id => ((QueueContext)SynchronizationContext.Current!).ThreadId;

        public static void Run(Func<Task> test)
        {
            Exception? failure = null;
            var thread = new Thread(() =>
            {
                using var context = new QueueContext();
                SynchronizationContext.SetSynchronizationContext(context);
                var task = test();
                task.ContinueWith(_ => context.Complete(), TaskScheduler.Default);
                context.RunUntilComplete();
                try
                {
                    task.GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            });

            thread.Start();
            if (!thread.Join(TimeSpan.FromMinutes(5)))
            {
                throw new TimeoutException("The test did not finish.");
            }

            if (failure != null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        private sealed class QueueContext : SynchronizationContext, IDisposable
        {
            private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> queue = new();

            public int ThreadId { get; } = Environment.CurrentManagedThreadId;

            public override void Post(SendOrPostCallback d, object? state)
            {
                try
                {
                    this.queue.Add((d, state));
                }
                catch (InvalidOperationException)
                {
                    // The test is over (the queue is completed or disposed); a stopped game
                    // waking up late runs where it is.
                    ThreadPool.QueueUserWorkItem(_ => d(state));
                }
            }

            public override void Send(SendOrPostCallback d, object? state) => throw new NotSupportedException();

            public void Complete() => this.queue.CompleteAdding();

            public void Dispose() => this.queue.Dispose();

            public void RunUntilComplete()
            {
                foreach (var (callback, state) in this.queue.GetConsumingEnumerable())
                {
                    callback(state);
                }
            }
        }
    }
}
