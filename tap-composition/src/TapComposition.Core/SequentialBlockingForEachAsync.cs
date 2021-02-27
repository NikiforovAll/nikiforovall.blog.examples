namespace TapComposition.Core
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public static class SequentialBlockingForEachAsync
    {
        public static async Task ForEachAsync<TResult, TSource>(
            this IEnumerable<TSource> list,
            Func<TSource, Task<TResult>> taskSelector,
            Action<TSource, TResult> resultProcessor)
        {
            foreach (var value in list)
            {
                resultProcessor(value, await taskSelector(value));
            }
        }
    }
}
