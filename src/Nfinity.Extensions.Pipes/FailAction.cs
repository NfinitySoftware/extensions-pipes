namespace Nfinity.Extensions.Pipes
{
    internal class FailAction
    {
        public readonly static FailAction Empty = new();

        public bool IsEmpty { get; }
        public Func<Task> Action { get; init; }
        public Func<OperationResult, Task> ActionWithResultArg { get; init; }

        private FailAction()
        {
            IsEmpty = true;
        }

        public FailAction(Func<Task> action)
        {
            Action = action;
        }

        public FailAction(Func<OperationResult, Task> actionWithResultArg)
        {
            ActionWithResultArg = actionWithResultArg;
        }
    }
}
