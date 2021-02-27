namespace TapComposition.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    using static TapComposition.Core.ExampleJobFactory;

    public class RunWhenAllTests
    {
        [Fact]
        public async Task WhenAll_Result_Unwrapped()
        {
            var job1 = RunJob(100, 1);
            var job2 = RunJob(300, 2);
            Task<int[]> whenAllTask = Task.WhenAll(job1, job2);
            var results = await whenAllTask;

            Assert.Equal(new int[] { 1, 2 }, results);
            Assert.True(whenAllTask.IsCompletedSuccessfully);
            Assert.Equal(TaskStatus.RanToCompletion, whenAllTask.Status);
        }

        [Fact]
        public void WhenAll_ExceptionsThrown_ExceptionsAggregated()
        {
            var job1 = RunJob(100, 1);
            var job2 = ThrowJob(100);
            var job3 = ThrowJob(100);
            var job4 = RunJob(150, 4);
            AggregateException captured = default;
            var whenAllTask = Task.WhenAll(job1, job2, job3, job4);
            try
            { whenAllTask.Wait(); }
            catch (AggregateException ae) { captured = ae; };

            var whenAllException = whenAllTask.Exception;
            Assert.True(whenAllTask.IsFaulted);
            Assert.Equal(2, captured.Flatten().InnerExceptions.Count);
            // this is interesting
            Assert.NotSame(whenAllException, captured);
            Assert.Equal(whenAllException.Message, captured.Message);
        }

        [Fact]
        public async Task WhenAll_ExceptionsThrown_ExceptionUnwrapped()
        {
            var job1 = RunJob(100, 1);
            var job2 = ThrowJob(100);
            var job3 = ThrowJob(100);
            List<Task> batch = new() { job1, job2, job3 };
            InvalidOperationException captured = default;

            // t_all = whenall(t1, t2->throws, t3->throws)
            // t_all.Exception = AggregateException(t2.Exception, t3.Exception)
            // await t_all -> unwrap t_all.AggregateException -> select (t2 | t3) as t_selected
            // -> unwrap t_selected

            var whenAllTask = Task.WhenAll(batch);
            try
            { await whenAllTask; }
            catch (InvalidOperationException e) { captured = e; };

            Assert.True(whenAllTask.IsFaulted);
            Assert.Equal(TaskStatus.Faulted, whenAllTask.Status);
            Assert.Equal(TaskStatus.RanToCompletion, job1.Status);

            Assert.Equal(2, whenAllTask.Exception.Flatten().InnerExceptions.Count);
            var aggregatedException = Assert.IsType<AggregateException>(job2.Exception);
            Assert.Contains(captured, batch.Select(t => t.Exception?.InnerException));
        }

        [Fact]
        public async Task WhenAll_ExceptionsThrown_UnwrappedChildTask()
        {
            var job1 = RunJob(100, 1);
            var job2 = ThrowJob(100);
            List<Task> batch = new() { job1, job2 };
            InvalidOperationException captured1 = default;
            InvalidOperationException captured2 = default;

            var whenAllTask = Task.WhenAll(batch);
            try
            { await whenAllTask; }
            catch (InvalidOperationException e) { captured1 = e; };

            Assert.Equal(TaskStatus.Faulted, job2.Status);
            try
            { await job2; }
            catch (InvalidOperationException e) { captured2 = e; };

            Assert.Same(captured1, captured2);
            Assert.Equal(captured1.Message, captured2.Message);
        }

        [Fact]
        public async Task WhenAll_CanceledTask_ExceptionThrown()
        {
            var job1 = RunJob(100, 1);
            var job2 = CanceledJob(50);
            var job3 = CanceledJob(100);

            TaskCanceledException captured = null;

            var whenAllTask = Task.WhenAll(job1, job2, job3);
            try
            { await whenAllTask; }
            catch (TaskCanceledException e) { captured = e; };

            Assert.True(whenAllTask.IsCompleted && whenAllTask.IsCanceled);
            Assert.Equal(TaskStatus.Canceled, whenAllTask.Status);
            Assert.NotNull(captured);
            Assert.Null(whenAllTask.Exception);
            Assert.Null(job2.Exception);
            Assert.Null(job3.Exception);
            Assert.Equal(TaskStatus.Canceled, job2.Status);
            Assert.Equal(TaskStatus.Canceled, job3.Status);
        }

        [Fact]
        public async Task WhenAll_CanceledAndFaulted_ExceptionOverCancellation()
        {
            var job1 = RunJob(100, 1);
            var job2 = CanceledJob(50);
            var job3 = ThrowJob(100);

            var whenAllTask = Task.WhenAll(job1, job2, job3);
            try
            { await whenAllTask; }
            catch { };

            Assert.True(whenAllTask.IsFaulted);
            Assert.Equal(TaskStatus.Faulted, whenAllTask.Status);
            Assert.Equal(TaskStatus.RanToCompletion, job1.Status);
            Assert.Equal(TaskStatus.Canceled, job2.Status);
            Assert.Equal(TaskStatus.Faulted, job3.Status);
            Assert.IsType<InvalidOperationException>(whenAllTask.Exception.InnerException);
            Assert.Null(job2.Exception);
        }

        [Fact]
        public async Task WhenAll_SharedCancellationToken_AllCancelled()
        {
            var cancellationTokenSource = new CancellationTokenSource(50);
            var t = cancellationTokenSource.Token;
            var job1 = RunJob(100, 1, t);
            var job2 = RunJob(150, 2, t);
            var job3 = RunJob(200, 3, t);
            List<Task> batch = new() { job1, job2, job3 };

            var whenAllTask = Task.WhenAll(batch);
            try
            { await whenAllTask; }
            catch { };

            Assert.All(batch, t => Assert.True(t.IsCanceled));
        }


        [Fact]
        public async Task WhenAny_FirstCanceled()
        {
            var job1 = RunJob(100, 1);
            var job2 = CanceledJob(50);
            var job3 = ThrowJob(70);
            List<Task> batch = new() { job1, job2, job3 };

            var waitForAnyTaskTask = Task.WhenAny(batch);
            var someTask = await waitForAnyTaskTask;

            Assert.Equal(TaskStatus.RanToCompletion, waitForAnyTaskTask.Status);
            Assert.Equal(TaskStatus.Canceled, someTask.Status);

            var someTask2 = await Task.WhenAny(batch);
            Assert.Same(someTask, someTask2);
            await Task.Delay(100); // let other tasks to complete
            var someTask3 = await Task.WhenAny(batch);

            Assert.NotSame(someTask, someTask3);
        }
    }
}
