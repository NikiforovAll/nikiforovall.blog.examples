namespace TapComposition.Tests
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using TapComposition.Core;
    using Xunit;
    using static TapComposition.Core.ExampleJobFactory;

    public class RunWhenAllThrottledTests
    {
        [Fact]
        public async Task WhenAllThrottled_AllGood()
        {
            var batch = Generate();
            var whenAllTask = batch.WhenAllThrottled(2);

            Assert.Equal(TaskStatus.WaitingForActivation, whenAllTask.Status);

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            await whenAllTask;
            stopWatch.Stop();

            Assert.Equal(TaskStatus.RanToCompletion, whenAllTask.Status);

            static IEnumerable<Task> Generate()
            {
                yield return RunJob(200, 1);
                yield return RunJob(200, 3);
                yield return RunJob(100, 2);
                yield return RunJob(100, 3);
            }
        }

        [Fact]
        public async Task WhenAllThrottled_ExceptionPropagated()
        {
            var batch = Generate();
            var whenAllTask = batch.WhenAllThrottled(2);

            try
            { await whenAllTask; }
            catch { };

            Assert.Equal(TaskStatus.Faulted, whenAllTask.Status);

            static IEnumerable<Task> Generate()
            {
                yield return RunJob(100, 1);
                yield return ThrowJob(100);
                yield return RunJob(100, 3);
                yield return ThrowJob(100);
                yield return RunJob(100, 5);
            }
        }

        [Fact]
        public async Task WhenAllThrottled_CancellationPropagated()
        {
            var batch = Generate();
            var whenAllTask = batch.WhenAllThrottled(2);

            try
            { await whenAllTask; }
            catch { };

            Assert.Equal(TaskStatus.Canceled, whenAllTask.Status);

            static IEnumerable<Task> Generate()
            {
                yield return RunJob(100, 1);
                yield return CanceledJob(100);
                yield return RunJob(100, 3);
                yield return CanceledJob(100);
            }
        }
    }
}
