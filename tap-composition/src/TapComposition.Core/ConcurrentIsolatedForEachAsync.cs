namespace TapComposition.Core
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public static class ConcurrentIsolatedForEachAsync
    {
        public static Task ForEachAsync<TSource, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, Task<TResult>> taskSelector, Action<TSource, TResult> resultProcessor)
        {
            SemaphoreSlim oneAtATime = new(initialCount: 1, maxCount: 1);
            return Task.WhenAll(
                from item in source
                select ProcessAsync(item, taskSelector, resultProcessor, oneAtATime));
        }

        private static async Task ProcessAsync<TSource, TResult>(
            TSource item,
            Func<TSource, Task<TResult>> taskSelector, Action<TSource, TResult> resultProcessor,
            SemaphoreSlim oneAtATime)
        {
            TResult result = await taskSelector(item);
            await oneAtATime.WaitAsync();
            try
            { resultProcessor(item, result); }
            finally { oneAtATime.Release(); }
        }
    }
}
