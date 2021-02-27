namespace TapComposition.Core
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class ExampleJobFactory
    {
        public static async Task<int> RunJob(int milliseconds, int taskId, CancellationToken cancellationToken = default)
        {
            await Task.Delay(milliseconds, cancellationToken);
            return taskId;
        }

        public static async Task ThrowJob(int milliseconds = 100)
        {
            await Task.Delay(milliseconds);
            throw new InvalidOperationException(nameof(ThrowJob));
        }

        public static async Task CanceledJob(int milliseconds)
        {
            var cancellationTokenSource = new CancellationTokenSource(milliseconds);
            await Task.Delay(milliseconds * 2, cancellationTokenSource.Token);
        }
    }
}
