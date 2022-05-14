using REvent.Utility;

namespace REvent
{
    public class Broker
    {
        private readonly MutableLookup<Type, IHandler> _handlerLookup = new();
        private readonly HashSet<object> _subscribedIdempotencyKeys = new();

        private void Subscribe(IHandler handler)
        {
            if (handler.IdempotencyKey != null)
            {
                if (_subscribedIdempotencyKeys.Contains(handler.IdempotencyKey))
                    return;

                _subscribedIdempotencyKeys.Add(handler.IdempotencyKey);
            }

            _handlerLookup.Add(handler.SubjectType, handler);
        }

        public ISubscriptionBuilder<T> On<T>() => new SubscriptionBuilder<T>(Subscribe, Unsubscribe);

        /// <summary>
        /// Unsubscribes the given handler. May be null, in which case the statement is ignored. (Convenience)
        /// </summary>
        public void Unsubscribe(IHandler? handler)
        {
            if (handler == null)
                return;

            _handlerLookup.Remove(handler.SubjectType, handler);

            if (handler.IdempotencyKey != null)
                _subscribedIdempotencyKeys.Remove(handler.IdempotencyKey);
        }

        /// <summary>
        /// Publishes an event, executing subscribed handlers in order of priority.
        /// </summary>
        /// <param name="event">Event to publish.</param>
        /// <returns>Returns the published event after all handlers have been executed.</returns>
        public T Publish<T>(T @event)
        {
            var handlers = GetTypeAndSupertypes(typeof(T))
                .SelectMany(t => _handlerLookup[t])
                .Cast<IHandler<T>>()
                .OrderBy(x => x.Priority)
                .ToList();

            foreach (var eventHandler in handlers)
                if (eventHandler.ShouldHandle(@event))
                {
                    if (eventHandler.IsOneTimeHandler)
                        Unsubscribe(eventHandler);

                    eventHandler.Handle(@event);
                }

            return @event;
        }

        private static IEnumerable<Type> GetTypeAndSupertypes(Type type)
        {
            Type? current = type;
            do
            {
                yield return current;
                current = current.BaseType;
            } while (current != null);
        }
    }
}