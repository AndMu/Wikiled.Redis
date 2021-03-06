using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive;
using Castle.Components.DictionaryAdapter;
using Microsoft.Reactive.Testing;
using Wikiled.Redis.Replication;

namespace Wikiled.Redis.UnitTests.Replication
{
    [TestFixture]
    public class LoggingProgressTrackerTests : ReactiveTest
    {
        private TestScheduler scheduler;

        private LoggingProgressTracker instance;

        private List<string> messages;

        [SetUp]
        public void Setup()
        {
            scheduler = new TestScheduler();
            messages = new EditableList<string>();
            instance = CreateLogginProgressTracker();
        }

        [Test]
        public void Track()
        {
            var progress = ReplicationProgress.CreateActive(
                new HostStatus(new DnsEndPoint("Test", 1), 100),
                new HostStatus(new DnsEndPoint("Test", 1), 100));
            var observable = scheduler.CreateHotObservable(
                new Recorded<Notification<ReplicationProgress>>(0, Notification.CreateOnNext(progress)),
                new Recorded<Notification<ReplicationProgress>>(1, Notification.CreateOnNext(progress)),
                new Recorded<Notification<ReplicationProgress>>(TimeSpan.FromSeconds(3).Ticks, Notification.CreateOnNext(progress)));
            instance.Track(observable);
            var oneStep = TimeSpan.FromSeconds(1).Ticks;
            scheduler.AdvanceBy(oneStep + 1);
            Assert.AreEqual(1, messages.Count);
            scheduler.AdvanceBy(oneStep);
            Assert.AreEqual(1, messages.Count);
            scheduler.AdvanceBy(oneStep * 2);
            Assert.AreEqual(2, messages.Count);
        }

        [Test]
        public void Construct()
        {
            Assert.Throws<ArgumentNullException>(
                () => new LoggingProgressTracker(
                    null,
                    TimeSpan.FromMinutes(1),
                    s => { }));

            Assert.Throws<ArgumentNullException>(
                () => new LoggingProgressTracker(
                    scheduler,
                    TimeSpan.FromMinutes(1),
                    null));
        }

        private LoggingProgressTracker CreateLogginProgressTracker()
        {
            return new LoggingProgressTracker(
                scheduler,
                TimeSpan.FromSeconds(1),
                item => messages.Add(item));
        }
    }
}
