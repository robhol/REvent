using REvent;
using FluentAssertions;
using Xunit;

namespace REvent.Test
{
    public class BrokerTests
    {
        class Event { }

        class StopEvent { }

        class MoreSpecificEvent : Event { }

        [Fact]
        public void FiresHandler_WhenNoPredicateIsGiven()
        {
            var broker = new Broker();
            var triggered = false;

            broker.On<Event>().Do(_ => triggered = true);

            broker.Publish(new Event());

            triggered.Should().BeTrue();
        }

        [Fact]
        public void FiresHandler_WhenPredicateIsGiven_AndPredicateIsTrue()
        {
            var broker = new Broker();
            var triggered = false;

            broker.On<Event>()
                .When(_ => true)
                .Do(_ => triggered = true);

            broker.Publish(new Event());

            triggered.Should().BeTrue();
        }

        [Fact]
        public void DoesNotFireHandler_WhenPredicateIsGiven_AndPredicateIsFalse()
        {
            var broker = new Broker();
            var triggered = false;

            broker.On<Event>()
                .When(_ => false)
                .Do(_ => triggered = true);

            broker.Publish(new Event());

            triggered.Should().BeFalse();
        }

        [Fact]
        public void DoesNotFireHandler_WhenUnsubscribed()
        {
            var broker = new Broker();
            var triggered = false;

            var handler = broker.On<Event>().Do(_ => triggered = true);

            broker.Unsubscribe(handler);

            broker.Publish(new Event());

            triggered.Should().BeFalse();
        }

        [Fact]
        public void FiresOneTimeHandlerOnce()
        {
            var broker = new Broker();
            var triggerCount = 0;

            broker.On<Event>()
                .Once()
                .Do(_ => triggerCount++);


            broker.Publish(new Event());
            broker.Publish(new Event());

            triggerCount.Should().Be(1);
        }

        [Fact]
        public void FiresHandler_ForSupertypesOfSubjectType()
        {
            var broker = new Broker();
            var triggerCount = 0;

            broker.On<Event>()
                .Do(_ => triggerCount++);

            broker.On<object>()
                .Do(_ => triggerCount++);

            broker.Publish(new MoreSpecificEvent());

            triggerCount.Should().Be(2);
        }
        
        [Fact]
        public void Unsubscribes_WhenStopEventIsPublished()
        {
            var broker = new Broker();

            var triggerCount = 0;

            broker.On<Event>()
                .Until<StopEvent>()
                .Do(_ => triggerCount++);

            broker.Publish(new Event());
            broker.Publish(new StopEvent());
            broker.Publish(new Event());

            triggerCount.Should().Be(1);
        }

        [Fact]
        public void Unsubscribes_WhenStopEventIsPublished_AndStopPredicateIsTrue()
        {
            var broker = new Broker();

            var triggerCount = 0;

            broker.On<Event>()
                .Until<StopEvent>(_ => _.When(x => true))
                .Do(_ => triggerCount++);

            broker.Publish(new Event());
            broker.Publish(new StopEvent());
            broker.Publish(new Event());

            triggerCount.Should().Be(1);
        }

        [Fact]
        public void DoesNotUnsubscribe_WhenStopEventIsPublished_AndStopPredicateIsFalse()
        {
            var broker = new Broker();

            var triggerCount = 0;

            broker.On<Event>()
                .Until<StopEvent>(_ => _.When(x => false))
                .Do(_ => triggerCount++);

            broker.Publish(new Event());
            broker.Publish(new StopEvent());
            broker.Publish(new Event());

            triggerCount.Should().Be(2);
        }

        [Fact]
        public void Unsubscribes_WhenExistingHandlerUsedAsStopIsTriggered()
        {
            var broker = new Broker();

            var triggerCount = 0;

            var stopHandler = broker.On<StopEvent>().Do(_ => { });

            broker.On<Event>()
                .Until(stopHandler)
                .Do(_ => triggerCount++);

            broker.Publish(new Event());
            broker.Publish(new StopEvent());
            broker.Publish(new Event());

            triggerCount.Should().Be(1);
        }

        [Fact]
        public void UnsubscribesHandlers_WhenstopHandlerIsReused()
        {
            var broker = new Broker();

            var triggerCount = 0;

            broker.On<Event>()
                .Until<StopEvent>(_ => _, out var stopHandler)
                .Do(_ => triggerCount++);

            broker.On<Event>()
                .Until(stopHandler)
                .Do(_ => triggerCount++);

            broker.Publish(new Event());
            broker.Publish(new StopEvent());
            broker.Publish(new Event());

            triggerCount.Should().Be(2);
        }

        [Fact]
        public void SubscribesIdempotentHandler_WhenIdempotencyKeyIsNotSubscribed()
        {
            var broker = new Broker();
            var triggerCount = 0;

            broker.On<Event>()
                .WithIdempotencyKey(123)
                .Do(_ => triggerCount++);

            broker.Publish(new Event());

            triggerCount.Should().Be(1);
        }

        [Fact]
        public void DoesNotSubscribeIdempotentHandler_WhenIdempotencyKeyIsAlreadySubscribed()
        {
            var broker = new Broker();
            var triggerCount = 0;

            broker.On<Event>()
                .WithIdempotencyKey(123)
                .Do(_ => triggerCount++);

            broker.On<Event>()
                .WithIdempotencyKey(123)
                .Do(_ => triggerCount++);

            broker.Publish(new Event());

            triggerCount.Should().Be(1);
        }

        [Fact]
        public void SubscribesIdempotentHandler_WhenPreviousIdempotentHandlerHasBeenUnsubscribed()
        {
            var broker = new Broker();
            var triggerCount = 0;

            var firstHandler = broker.On<Event>()
                .WithIdempotencyKey(123)
                .Do(_ => triggerCount++);

            broker.Unsubscribe(firstHandler);

            broker.On<Event>()
                .WithIdempotencyKey(123)
                .Do(_ => triggerCount++);

            broker.Publish(new Event());

            triggerCount.Should().Be(1);
        }
    }
}
