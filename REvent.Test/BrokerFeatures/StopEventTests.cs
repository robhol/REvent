using FluentAssertions;
using REvent.Test.ExampleData;
using Xunit;

namespace REvent.Test.BrokerFeatures
{
    public class StopEventTests
    {
        [Fact]
        public void Unsubscribes_WhenStopEventIsPublished()
        {
            var broker = new Broker();

            var triggerCount = 0;

            broker.On<GenericEvent>()
                .Until<StopEvent>()
                .Do(_ => triggerCount++);

            broker.Publish(new GenericEvent());
            broker.Publish(new StopEvent());
            broker.Publish(new GenericEvent());

            triggerCount.Should().Be(1);
        }

        [Fact]
        public void Unsubscribes_WhenStopEventIsPublished_AndStopPredicateIsTrue()
        {
            var broker = new Broker();

            var triggerCount = 0;

            broker.On<GenericEvent>()
                .Until<StopEvent>(_ => _.When(x => true))
                .Do(_ => triggerCount++);

            broker.Publish(new GenericEvent());
            broker.Publish(new StopEvent());
            broker.Publish(new GenericEvent());

            triggerCount.Should().Be(1);
        }

        [Fact]
        public void DoesNotUnsubscribe_WhenStopEventIsPublished_AndStopPredicateIsFalse()
        {
            var broker = new Broker();

            var triggerCount = 0;

            broker.On<GenericEvent>()
                .Until<StopEvent>(_ => _.When(x => false))
                .Do(_ => triggerCount++);

            broker.Publish(new GenericEvent());
            broker.Publish(new StopEvent());
            broker.Publish(new GenericEvent());

            triggerCount.Should().Be(2);
        }

        [Fact]
        public void Unsubscribes_WhenExistingHandlerUsedAsStopIsTriggered()
        {
            var broker = new Broker();

            var triggerCount = 0;

            var stopHandler = broker.On<StopEvent>().Do(_ => { });

            broker.On<GenericEvent>()
                .Until(stopHandler)
                .Do(_ => triggerCount++);

            broker.Publish(new GenericEvent());
            broker.Publish(new StopEvent());
            broker.Publish(new GenericEvent());

            triggerCount.Should().Be(1);
        }

        [Fact]
        public void UnsubscribesHandlers_WhenstopHandlerIsReused()
        {
            var broker = new Broker();

            var triggerCount = 0;

            broker.On<GenericEvent>()
                .Until<StopEvent>(_ => _, out var stopHandler)
                .Do(_ => triggerCount++);

            broker.On<GenericEvent>()
                .Until(stopHandler)
                .Do(_ => triggerCount++);

            broker.Publish(new GenericEvent());
            broker.Publish(new StopEvent());
            broker.Publish(new GenericEvent());

            triggerCount.Should().Be(2);
        }
    }
}
