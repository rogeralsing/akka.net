//-----------------------------------------------------------------------
// <copyright file="FlowSelectAsyncSpec.cs" company="Akka.NET Project">
//     Copyright (C) 2015-2016 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Streams.Dsl;
using Akka.Streams.Implementation;
using Akka.Streams.Supervision;
using Akka.Streams.TestKit;
using Akka.Streams.TestKit.Tests;
using Akka.TestKit;
using Akka.TestKit.Internal;
using Akka.Util;
using Akka.Util.Internal;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable InvokeAsExtensionMethod
#pragma warning disable 162

namespace Akka.Streams.Tests.Dsl
{
    public class FlowSelectAsyncSpec : TestKit.Tests.AkkaSpec
    {
        private ActorMaterializer Materializer { get; }

        public FlowSelectAsyncSpec(ITestOutputHelper helper) : base(helper)
        {
            Materializer = ActorMaterializer.Create(Sys);
        }

        [Fact]
        public void A_Flow_with_SelectAsync_must_produce_task_elements()
        {
            this.AssertAllStagesStopped(() =>
            {
                var c = TestSubscriber.CreateManualProbe<int>(this);
                Source.From(Enumerable.Range(1, 3))
                    .SelectAsync(4, Task.FromResult)
                    .RunWith(Sink.FromSubscriber(c), Materializer);
                var sub = c.ExpectSubscription();

                sub.Request(2);
                c.ExpectNext(1)
                    .ExpectNext(2)
                    .ExpectNoMsg(TimeSpan.FromMilliseconds(200));
                sub.Request(2);

                c.ExpectNext(3)
                    .ExpectComplete();
            }, Materializer);
        }

        [Fact]
        public void A_Flow_with_SelectAsync_must_produce_task_elements_in_order()
        {
            var c = TestSubscriber.CreateManualProbe<int>(this);
            Source.From(Enumerable.Range(1, 50))
                .SelectAsync(4, i => Task.Run(()=>
                {
                    Thread.Sleep(ThreadLocalRandom.Current.Next(1, 10));
                    return i;
                }))
                .RunWith(Sink.FromSubscriber(c), Materializer);
            var sub = c.ExpectSubscription();
            sub.Request(1000);
            Enumerable.Range(1, 50).ForEach(n => c.ExpectNext(n));
            c.ExpectComplete();
        }

        [Fact]
        public void A_Flow_with_SelectAsync_must_not_run_more_futures_than_requested_parallelism()
        {
            var probe = CreateTestProbe();
            var c = TestSubscriber.CreateManualProbe<int>(this);
            Source.From(Enumerable.Range(1, 20))
                .SelectAsync(8, n => Task.Run(() => 
                {
                    probe.Ref.Tell(n);
                    return n;
                }))
                .RunWith(Sink.FromSubscriber(c), Materializer);
            var sub = c.ExpectSubscription();
            probe.ExpectNoMsg(TimeSpan.FromMilliseconds(500));
            sub.Request(1);
            probe.ReceiveN(9).ShouldAllBeEquivalentTo(Enumerable.Range(1, 9));
            probe.ExpectNoMsg(TimeSpan.FromMilliseconds(500));
            sub.Request(2);
            probe.ReceiveN(2).ShouldAllBeEquivalentTo(Enumerable.Range(10, 2));
            probe.ExpectNoMsg(TimeSpan.FromMilliseconds(500));
            sub.Request(10);
            probe.ReceiveN(9).ShouldAllBeEquivalentTo(Enumerable.Range(12, 9));
            probe.ExpectNoMsg(TimeSpan.FromMilliseconds(200));

            Enumerable.Range(1, 13).ForEach(n => c.ExpectNext(n));
            c.ExpectNoMsg(TimeSpan.FromMilliseconds(200));
        }

        [Fact]
        public void A_Flow_with_SelectAsync_must_signal_task_failure()
        {
            this.AssertAllStagesStopped(() =>
            {
                var latch = new TestLatch(1);
                var c = TestSubscriber.CreateManualProbe<int>(this);
                Source.From(Enumerable.Range(1, 5))
                    .SelectAsync(4, n => Task.Run(() =>
                    {
                        if (n == 3)
                            throw new TestException("err1");

                        latch.Ready(TimeSpan.FromSeconds(10));
                        return n;
                    }))
                    .To(Sink.FromSubscriber(c)).Run(Materializer);
                var sub = c.ExpectSubscription();
                sub.Request(10);
                c.ExpectError().InnerException.Message.Should().Be("err1");
                latch.CountDown();
            }, Materializer);
        }

        [Fact]
        public void A_Flow_with_SelectAsync_must_signal_error_from_MapAsync()
        {
            this.AssertAllStagesStopped(() =>
            {
                var latch = new TestLatch(1);
                var c = TestSubscriber.CreateManualProbe<int>(this);
                Source.From(Enumerable.Range(1, 5))
                    .SelectAsync(4, n =>
                    {
                        if (n == 3)
                            throw new TestException("err2");

                        return Task.Run(() =>
                        {
                            latch.Ready(TimeSpan.FromSeconds(10));
                            return n;
                        });
                    })
                    .RunWith(Sink.FromSubscriber(c), Materializer);
                var sub = c.ExpectSubscription();
                sub.Request(10);
                c.ExpectError().Message.Should().Be("err2");
                latch.CountDown();
            }, Materializer);
        }

        [Fact]
        public void A_Flow_with_SelectAsync_must_resume_after_task_failure()
        {
            this.AssertAllStagesStopped(() =>
            {
                this.AssertAllStagesStopped(() =>
                {
                    var c = TestSubscriber.CreateManualProbe<int>(this);
                    Source.From(Enumerable.Range(1, 5))
                        .SelectAsync(4, n => Task.Run(() =>
                        {
                            if (n == 3)
                                throw new TestException("err3");
                            return n;
                        }))
                        .WithAttributes(ActorAttributes.CreateSupervisionStrategy(Deciders.ResumingDecider))
                        .RunWith(Sink.FromSubscriber(c), Materializer);
                    var sub = c.ExpectSubscription();
                    sub.Request(10);
                    new[] {1, 2, 4, 5}.ForEach(i => c.ExpectNext(i));
                    c.ExpectComplete();
                }, Materializer);
            }, Materializer);
        }

        [Fact]
        public void A_Flow_with_SelectAsync_must_resume_after_multiple_failures()
        {
            this.AssertAllStagesStopped(() =>
            {
                var futures = new[]
                {
                    Task.Run(() => { throw new TestException("failure1"); return "";}),
                    Task.Run(() => { throw new TestException("failure2"); return "";}),
                    Task.Run(() => { throw new TestException("failure3"); return "";}),
                    Task.Run(() => { throw new TestException("failure4"); return "";}),
                    Task.Run(() => { throw new TestException("failure5"); return "";}),
                    Task.FromResult("happy")
                };

                var t = Source.From(futures)
                    .SelectAsync(2, x => x)
                    .WithAttributes(ActorAttributes.CreateSupervisionStrategy(Deciders.ResumingDecider))
                    .RunWith(Sink.First<string>(), Materializer);

                t.Wait(TimeSpan.FromSeconds(3)).Should().BeTrue();
                t.Result.Should().Be("happy");
            }, Materializer);
        }

        [Fact]
        public void A_Flow_with_SelectAsync_must_finish_after_task_failure()
        {
            this.AssertAllStagesStopped(() =>
            {
                var t = Source.From(Enumerable.Range(1, 3))
                    .SelectAsync(1, n => Task.Run(() =>
                    {
                        if (n == 3)
                            throw new TestException("err3b");
                        return n;
                    }))
                    .WithAttributes(ActorAttributes.CreateSupervisionStrategy(Deciders.ResumingDecider))
                    .Grouped(10)
                    .RunWith(Sink.First<IEnumerable<int>>(), Materializer);

                t.Wait(TimeSpan.FromSeconds(1)).Should().BeTrue();
                t.Result.ShouldAllBeEquivalentTo(new[] {1, 2});
            }, Materializer);
        }

        [Fact]
        public void A_Flow_with_SelectAsync_must_resume_when_SelectAsync_throws()
        {
            var c = TestSubscriber.CreateManualProbe<int>(this);
            Source.From(Enumerable.Range(1, 5))
                .SelectAsync(4, n =>
                {
                    if (n == 3)
                        throw new TestException("err4");
                    return Task.FromResult(n);
                })
                .WithAttributes(ActorAttributes.CreateSupervisionStrategy(Deciders.ResumingDecider))
                .RunWith(Sink.FromSubscriber(c), Materializer);
            var sub = c.ExpectSubscription();
            sub.Request(10);
            new[] {1, 2, 4, 5}.ForEach(i => c.ExpectNext(i));
            c.ExpectComplete();
        }

        [Fact]
        public void A_Flow_with_SelectAsync_must_signal_NPE_when_task_is_completed_with_null()
        {
            var c = TestSubscriber.CreateManualProbe<string>(this);

            Source.From(new[] {"a", "b"})
                .SelectAsync(4, _ => Task.FromResult(null as string))
                .To(Sink.FromSubscriber(c)).Run(Materializer);

            var sub = c.ExpectSubscription();
            sub.Request(10);
            c.ExpectError().Message.Should().StartWith(ReactiveStreamsCompliance.ElementMustNotBeNullMsg);
        }

        [Fact]
        public void A_Flow_with_SelectAsync_must_resume_when_task_is_completed_with_null()
        {
            var c = TestSubscriber.CreateManualProbe<string>(this);
            Source.From(new[] { "a", "b", "c" })
                .SelectAsync(4, s => s.Equals("b") ? Task.FromResult(null as string) : Task.FromResult(s))
                .WithAttributes(ActorAttributes.CreateSupervisionStrategy(Deciders.ResumingDecider))
                .To(Sink.FromSubscriber(c)).Run(Materializer);
            var sub = c.ExpectSubscription();
            sub.Request(10);
            c.ExpectNext("a");
            c.ExpectNext("c");
            c.ExpectComplete();
        }

        [Fact]
        public void A_Flow_with_SelectAsync_must_handle_cancel_properly()
        {
            this.AssertAllStagesStopped(() =>
            {
                var pub = TestPublisher.CreateManualProbe<int>(this);
                var sub = TestSubscriber.CreateManualProbe<int>(this);

                Source.FromPublisher(pub)
                    .SelectAsync(4, _ => Task.FromResult(0))
                    .RunWith(Sink.FromSubscriber(sub), Materializer);

                var upstream = pub.ExpectSubscription();
                upstream.ExpectRequest();

                sub.ExpectSubscription().Cancel();

                upstream.ExpectCancellation();
            }, Materializer);
        }

        [Fact]
        public void A_Flow_with_SelectAsync_must_not_run_more_futures_than_configured()
        {
            this.AssertAllStagesStopped(() =>
            {
                const int parallelism = 8;
                var counter = new AtomicCounter();
                var queue = new BlockingQueue<Tuple<TaskCompletionSource<int>, long>>();
                var cancellation = new CancellationTokenSource();
                var token = cancellation.Token;

                var timer = new Task(() =>
                {
                    var delay = 500; // 50000 nanoseconds
                    var count = 0;
                    var cont = true;
                    while (cont)
                    {
                        if (token.IsCancellationRequested)
                            break;

                        try
                        {
                            var t = queue.Take(CancellationToken.None);
                            var promise = t.Item1;
                            var enqueued = t.Item2;
                            var wakeup = enqueued + delay;
                            while (DateTime.Now.Ticks < wakeup) { }
                            counter.Decrement();
                            promise.SetResult(count);
                            count++;
                        }
                        catch
                        {
                            cont = false;
                        }
                    }
                }, token);

                timer.Start();

                Func<Task<int>> deferred = () =>
                {
                    var promise = new TaskCompletionSource<int>();
                    if (counter.IncrementAndGet() > parallelism)
                        promise.SetException(new Exception("parallelism exceeded"));
                    else

                        queue.Enqueue(Tuple.Create(promise, DateTime.Now.Ticks));
                    return promise.Task;
                };

                try
                {
                    const int n = 10000;
                    var task = Source.From(Enumerable.Range(1, n))
                        .SelectAsync(parallelism, _ => deferred())
                        .RunAggregate(0, (c, _) => c + 1, Materializer);

                    task.Wait(TimeSpan.FromSeconds(3)).Should().BeTrue();
                    task.Result.Should().Be(n);
                }
                finally
                {
                    cancellation.Cancel();
                }
            }, Materializer);
        }
    }
}
