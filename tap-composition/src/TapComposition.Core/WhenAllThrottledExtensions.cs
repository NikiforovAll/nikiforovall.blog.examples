namespace TapComposition.Core
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    public static class WhenAllThrottledExtensions
    {
        public static async Task WhenAllThrottled(this IEnumerable<Task> source, int throttled)
        {
            var tasks = new List<Task>();
            throttled--;
            foreach (var task in source)
            {
                if (tasks.Count == throttled)
                {
                    var finishedTask = await Task.WhenAny(tasks);
                    _ = tasks.Remove(finishedTask);
                }
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
        }
    }
}
