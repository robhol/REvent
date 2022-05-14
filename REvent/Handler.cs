namespace REvent
{
    public interface IHandler
    {
        Type SubjectType { get; }
        object? IdempotencyKey { get; }
        void Decorate(Action newBehavior);
    }

    internal abstract class Handler : IHandler
    {
        protected Handler(Type subjectType, object? idempotencyKey)
        {
            SubjectType = subjectType;
            IdempotencyKey = idempotencyKey;
        }

        public Type SubjectType { get; }
        public object? IdempotencyKey { get; }
        public abstract void Decorate(Action newBehavior);
    }

    public interface IHandler<in T> : IHandler
    {
        Priority Priority { get; }
        bool IsOneTimeHandler { get; }
        bool ShouldHandle(T subject);
        void Handle(T subject);
    }

    internal class Handler<T> : Handler, IHandler<T>
    {
        public Priority Priority { get; }
        public bool IsOneTimeHandler { get; }

        private readonly Func<T, bool>? _shouldHandlePredicate;
        private Action<T> _handlerAction;

        public Handler(Priority priority, Func<T, bool>? shouldHandlePredicate, Action<T> handlerAction, bool isOneTimeHandler, object? idempotencyKey) : base(typeof(T), idempotencyKey)
        {
            Priority = priority;
            IsOneTimeHandler = isOneTimeHandler;
            _shouldHandlePredicate = shouldHandlePredicate;
            _handlerAction = handlerAction;
        }

        public bool ShouldHandle(T subject) => _shouldHandlePredicate?.Invoke(subject) ?? true;

        public void Handle(T subject) => _handlerAction(subject);

        public override void Decorate(Action newBehavior)
        {
            var existingBehavior = _handlerAction;
            _handlerAction = subject =>
            {
                existingBehavior(subject);
                newBehavior();
            };
        }
    }
}