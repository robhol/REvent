using FluentAssertions;
using REvent.Test.ExampleData;
using Xunit;

namespace REvent.Test.BrokerFeatures
{
    public class IdempotencyTests
    {
        [Fact]
        public void SubscribesIdempotentHandler_WhenIdempotencyKeyIsNotSubscribed()
        {
            var broker = new Broker();
            var triggerCount = 0;

            broker.On<GenericEvent>()
                .WithIdempotencyKey(123)
                .Do(_ => triggerCount++);

            broker.Publish(new GenericEvent());

            triggerCount.Should().Be(1);
        }

        [Fact]
        public void DoesNotSubscribeIdempotentHandler_WhenIdempotencyKeyIsAlreadySubscribed()
        {
            var broker = new Broker();
            var triggerCount = 0;

            broker.On<GenericEvent>()
                .WithIdempotencyKey(123)
                .Do(_ => triggerCount++);

            broker.On<GenericEvent>()
                .WithIdempotencyKey(123)
                .Do(_ => triggerCount++);

            broker.Publish(new GenericEvent());

            triggerCount.Should().Be(1);
        }

        [Fact]
        public void SubscribesIdempotentHandler_WhenPreviousIdempotentHandlerHasBeenUnsubscribed()
        {
            var broker = new Broker();
            var triggerCount = 0;

            var firstHandler = broker.On<GenericEvent>()
                .WithIdempotencyKey(123)
                .Do(_ => triggerCount++);

            broker.Unsubscribe(firstHandler);

            broker.On<GenericEvent>()
                .WithIdempotencyKey(123)
                .Do(_ => triggerCount++);

            broker.Publish(new GenericEvent());

            triggerCount.Should().Be(1);
        }
    }
}
