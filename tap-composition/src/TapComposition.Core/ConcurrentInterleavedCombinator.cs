namespace TapComposition.Core
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public static class ConcurrentInterleavedCombinator
    {
        public static Task<Task<T>>[] Interleaved<T>(IEnumerable<Task<T>> tasks)
        {
            var inputTasks = tasks.ToList();

            var buckets = new TaskCompletionSource<Task<T>>[inputTasks.Count];
            var results = new Task<Task<T>>[buckets.Length];
            for (var i = 0; i < buckets.Length; i++)
            {
                buckets[i] = new TaskCompletionSource<Task<T>>();
                results[i] = buckets[i].Task;
            }

            var nextTaskIndex = -1;
            void continuation(Task<T> completed)
            {
                var bucket = buckets[Interlocked.Increment(ref nextTaskIndex)];
                _ = bucket.TrySetResult(completed);
            }

            foreach (var inputTask in inputTasks)
            {
                _ = inputTask.ContinueWith(
                    continuation,
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }

            return results;
        }
    }
}
