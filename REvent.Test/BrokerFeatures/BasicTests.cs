using FluentAssertions;
using REvent.Test.ExampleData;
using Xunit;

namespace REvent.Test.BrokerFeatures
{
    public class BasicTests
    {
        [Fact]
        public void FiresHandler_WhenNoPredicateIsGiven()
        {
            var broker = new Broker();
            var triggered = false;

            broker.On<GenericEvent>().Do(_ => triggered = true);

            broker.Publish(new GenericEvent());

            triggered.Should().BeTrue();
        }

        [Fact]
        public void FiresHandler_WhenPredicateIsGiven_AndPredicateIsTrue()
        {
            var broker = new Broker();
            var triggered = false;

            broker.On<GenericEvent>()
                .When(_ => true)
                .Do(_ => triggered = true);

            broker.Publish(new GenericEvent());

            triggered.Should().BeTrue();
        }

        [Fact]
        public void DoesNotFireHandler_WhenPredicateIsGiven_AndPredicateIsFalse()
        {
            var broker = new Broker();
            var triggered = false;

            broker.On<GenericEvent>()
                .When(_ => false)
                .Do(_ => triggered = true);

            broker.Publish(new GenericEvent());

            triggered.Should().BeFalse();
        }

        [Fact]
        public void DoesNotFireHandler_WhenUnsubscribed()
        {
            var broker = new Broker();
            var triggered = false;

            var handler = broker.On<GenericEvent>().Do(_ => triggered = true);

            broker.Unsubscribe(handler);

            broker.Publish(new GenericEvent());

            triggered.Should().BeFalse();
        }

        [Fact]
        public void FiresOneTimeHandlerOnce()
        {
            var broker = new Broker();
            var triggerCount = 0;

            broker.On<GenericEvent>()
                .Once()
                .Do(_ => triggerCount++);


            broker.Publish(new GenericEvent());
            broker.Publish(new GenericEvent());

            triggerCount.Should().Be(1);
        }

        [Fact]
        public void FiresHandler_ForSupertypesOfSubjectType()
        {
            var broker = new Broker();
            var triggerCount = 0;

            broker.On<GenericEvent>()
                .Do(_ => triggerCount++);

            broker.On<object>()
                .Do(_ => triggerCount++);

            broker.Publish(new MoreSpecificEvent());

            triggerCount.Should().Be(2);
        }

    }
}
