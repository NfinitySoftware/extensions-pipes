namespace Nfinity.Extensions.Pipes
{
    public sealed class PipedOperationResult
    {
        public PipedOperationOptions Options { get; }

        private readonly List<OperationResult> _results = [];
        public IReadOnlyList<OperationResult> Results => _results;

        private List<Func<Task>> _failActions;
        public IReadOnlyList<Func<Task>> FailActions => _failActions;

        private List<OperationResult> _failActionResults;
        public IReadOnlyList<OperationResult> FailActionResults => _failActionResults;

        private List<Task> _failActionTasks;
        public IReadOnlyList<Task> FailActionTasks => _failActionTasks;

        private Func<Task> _finalAction;
        public Func<Task> FinalAction => _finalAction;

        public OperationResult FinalResult { get; internal set; }

        internal bool HasExecutedFailActions { get; set; }

        internal PipedOperationResult()
        {
            Options = new PipedOperationOptions();
        }

        internal PipedOperationResult(PipeFailureBehavior failureBehaviour)
        {
            Options = new PipedOperationOptions(failureBehaviour);
        }

        public OperationFailureState GetFailureState()
        {
            var last = _results.LastOrDefault();
            if (last == null || last.IsSuccess) return null;

            var compiledResult = _results.CompileFailResult();
            return OperationFailureState.FromResult(compiledResult);
        }

        public bool IsSuccess()
            => _results.LastOrDefault()?.IsSuccess ?? true;

        internal void PushResult(OperationResult result, Task<OperationResult> task)
            => _results.Add(result);

        internal void PushFailActionResult(OperationResult result, Task task)
        {
            if (_failActionResults == null)
            {
                _failActionResults = [];
                _failActionTasks = [];
            }

            _failActionResults.Add(result);
            _failActionTasks.Add(task);
        }

        internal void PushFailAction(Func<Task> action)
        {
            _failActions ??= [];
            _failActions.Add(action);
        }

        internal void PushFinalAction(Func<Task> final)
        {
            if (_finalAction != null) throw new InvalidOperationException("A finally action has already been specified");
            _finalAction = final;
        }
    }
}
