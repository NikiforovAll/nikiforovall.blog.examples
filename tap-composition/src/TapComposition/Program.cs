#pragma warning disable CA1822
#pragma warning disable CA1050
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using TapComposition.Core;
using static TapComposition.Core.ConcurrentInterleavedCombinator;

BenchmarkRunner.Run<ForEachAsync>();

public class ForEachAsync
{
    private static IEnumerable<int> GenerateData()
    {
        yield return 2;
        yield return 1;
        yield return 2;
        yield return 1;
    }

    [Benchmark]
    public async Task SequentialBlocking() =>
        await SequentialBlockingForEachAsync.ForEachAsync(
            GenerateData(), async i =>
            {
                await Task.Delay(i * 100);
                return i;
            }, Empty);

    [Benchmark]
    public async Task ConcurrentIsolated() =>
        await ConcurrentIsolatedForEachAsync.ForEachAsync(
            GenerateData(), async i =>
            {
                await Task.Delay(i * 100);
                return i;
            }, Empty);

    [Benchmark]
    public async Task ConcurrentInterleavedCombinator()
    {
        var tasks = GenerateData().Select(async i =>
        {
            await Task.Delay(i * 100);
            return i;
        });

        foreach (var bucket in Interleaved(tasks))
        {
            var t = await bucket;
        }
    }

    [Benchmark]
    public async Task ThrottledInterleaved() =>
        await GenerateData().Select(i => Task.Delay(i * 100)).WhenAllThrottled(2);

    [Benchmark]
    public async Task ControlParallelismForEachAsync() =>
        await ControlParallelismForEachAsyncExtensions.ForEachAsync(
            GenerateData(), 2, i => Task.Delay(i * 100));

    private static void Empty<T>(T source, T result) { }
    // private static IEnumerable<DelayedWorkItem<int>> GenerateData()
    // {
    //     yield return new() { Delay = ms(200), Payload = 1 };
    //     yield return new() { Delay = ms(150), Payload = 2 };
    //     yield return new() { Delay = ms(100), Payload = 3 };
    //     yield return new() { Delay = ms(50), Payload = 4 };
    // }

    // [Benchmark]
    // public async Task SequentialBlocking() =>
    //     await SequentialBlockingForEachAsync.ForEachAsync(
    //         GenerateData(), Do, Process);

    // [Benchmark]
    // public async Task ConcurrentIsolated() =>
    //     await ConcurrentIsolatedForEachAsync.ForEachAsync(
    //         GenerateData(), Do, Process);

    // private static async Task<int> Do(DelayedWorkItem<int> source)
    // {
    //     await Task.Delay(source.Delay);
    //     return source.Payload;
    // }
    // private static void Process(DelayedWorkItem<int> source, int result)
    // {
    //     Console.WriteLine($"Result[{Thread.CurrentThread.ManagedThreadId}]: {result}");
    // }

    // private static Task<int> ProjectWithDelay(DelayedWorkItem<int> source) =>
    //     DelayCore(source, i => i);

    // private static async Task<TResult> DelayCore<TSource, TResult>(
    //     DelayedWorkItem<TSource> source, Func<TSource, TResult> process)
    // {
    //     await Task.Delay(source.Delay);
    //     return process(source.Payload);
    // }

    // private static TimeSpan ms(int m) => TimeSpan.FromMilliseconds(m);
}

public class DelayedWorkItem<T>
{
    public TimeSpan Delay { get; set; }

    public T Payload { get; set; }
}
