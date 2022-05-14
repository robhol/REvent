namespace REvent
{
    public interface ISimpleSubscriptionBuilder<T>
    {
        /// <summary>
        /// Specify a condition for this handler to trigger.
        /// </summary>
        ISubscriptionBuilder<T> When(Func<T, bool> predicate);

        /// <summary>
        /// Specify a priority for this handler.
        /// </summary>
        ISubscriptionBuilder<T> WithPriority(Priority priority);

        /// <summary>
        /// Specify an action to perform when the handler is triggered.
        /// </summary>
        IHandler Do(Action<T> handler);

        /// <summary>
        /// Get an empty handler that does nothing by itself.
        /// </summary>
        IHandler GetHandler() => Do(_ => { });
    }

    public interface ISubscriptionBuilder<T> : ISimpleSubscriptionBuilder<T>
    {
        /// <summary>
        /// Specify that the handler should only run once. It will then unsubscribe itself.
        /// </summary>
        ISubscriptionBuilder<T> Once();

        /// <summary>
        /// Specify that the handler should unsubscribe itself once a certain event is published.
        /// The handler for the stop event is output for further use.
        /// </summary>
        ISubscriptionBuilder<T> Until<TStopEvent>(StopEventBuilder<TStopEvent>? stopEventBuilder, out IHandler stopHandler);

        /// <summary>
        /// Specify that the handler should unsubscribe itself once an existing handler is triggered.
        /// </summary>
        ISubscriptionBuilder<T> Until(IHandler stopHandler);

        /// <summary>
        /// Specify that the handler should unsubscribe itself as a reaction to another event.
        /// </summary>
        ISubscriptionBuilder<T> Until<TStopEvent>(StopEventBuilder<TStopEvent>? stopEventBuilder = null) => Until(stopEventBuilder, out _);

        /// <summary>
        /// Specify that this handler is idempotent and that no other handlers with the same idempotency key should be subscribed.
        /// </summary>
        /// <param name="idempotencyKey">Idempotency key of arbitrary type.</param>
        /// <returns></returns>
        ISubscriptionBuilder<T> WithIdempotencyKey(object idempotencyKey);
    }

    public delegate ISimpleSubscriptionBuilder<TStopEvent> StopEventBuilder<TStopEvent>(ISimpleSubscriptionBuilder<TStopEvent> _);

    internal class SubscriptionBuilder<T> : ISubscriptionBuilder<T>
    {
        private readonly Action<Handler> _subscribeAction;
        private readonly Action<Handler> _unsubscribeAction;

        private readonly List<IHandler> _stopHandlers = new();

        private Func<T, bool>? _predicate = null;
        private Priority _priority = Priority.Normal;
        private bool _isOneTimeHandler = false;
        private object? _idempotencyKey = null;

        public SubscriptionBuilder(Action<Handler> subscribeAction, Action<Handler> unsubscribeAction)
        {
            _subscribeAction = subscribeAction;
            _unsubscribeAction = unsubscribeAction;
        }

        public ISubscriptionBuilder<T> When(Func<T, bool> predicate)
        {
            _predicate = predicate;
            return this;
        }

        public ISubscriptionBuilder<T> WithPriority(Priority priority)
        {
            _priority = priority;
            return this;
        }

        public ISubscriptionBuilder<T> Once()
        {
            _isOneTimeHandler = true;
            return this;
        }

        public ISubscriptionBuilder<T> Until<TStopEvent>(StopEventBuilder<TStopEvent>? stopEventBuilder, out IHandler stopHandler)
        {
            var builder = new SubscriptionBuilder<TStopEvent>(_subscribeAction, _unsubscribeAction);
            stopEventBuilder?.Invoke(builder);

            stopHandler = builder.Once().GetHandler();
            return Until(stopHandler);
        }

        public ISubscriptionBuilder<T> Until(IHandler stopHandler)
        {
            _stopHandlers.Add(stopHandler);
            return this;
        }

        public ISubscriptionBuilder<T> WithIdempotencyKey(object idempotencyKey)
        {
            _idempotencyKey = idempotencyKey;
            return this;
        }

        public IHandler Do(Action<T> action)
        {
            var handler = new Handler<T>(_priority, _predicate, action, _isOneTimeHandler, _idempotencyKey);
            _subscribeAction(handler);

            foreach (var stopHandler in _stopHandlers)
                stopHandler.Decorate(() => _unsubscribeAction(handler));

            return handler;
        }
    }
}