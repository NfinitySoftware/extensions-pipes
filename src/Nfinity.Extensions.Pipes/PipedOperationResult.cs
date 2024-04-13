namespace Nfinity.Extensions.Pipes
{
    /// <summary>
    /// Represents the overall result of an asynchronous pipeline.
    /// </summary>
    public sealed class PipedOperationResult
    {
        /// <summary>
        /// Gets a reference to a <see cref="PipedOperationOptions"/> instance, representing the
        /// options with which the pipeline was created.
        /// </summary>
        public PipedOperationOptions Options { get; }

        private readonly List<OperationResult> _results = [];
        /// <summary>
        /// Gets a reference to the list of pipeline results.
        /// </summary>
        public IReadOnlyList<OperationResult> Results => _results;

        private List<OperationResult> _failActionResults;
        /// <summary>
        /// Gets a reference to the list of results of all failure actions, if any were specified or run.
        /// </summary>
        public IReadOnlyList<OperationResult> FailActionResults => _failActionResults;

        /// <summary>
        /// Gets a reference to the result of the 'Finally' action, if one was specified.
        /// </summary>
        public OperationResult FinalResult { get; internal set; }

        private readonly List<FailAction> _failActions = [];
        internal IReadOnlyList<FailAction> FailActions => _failActions;

        private Func<Task> _finalAction;
        internal Func<Task> FinalAction => _finalAction;

        internal bool HasExecutedFailActions { get; set; }

        internal PipedOperationResult()
        {
            Options = new PipedOperationOptions();
        }

        internal PipedOperationResult(PipeFailureBehavior failureBehaviour)
        {
            Options = new PipedOperationOptions(failureBehaviour);
        }

        /// <summary>
        /// Compiles the results of all failed pipeline actions and returns a <see cref="OperationFailureState"/> instance representing the result.
        /// Returns null if no actions were run, or if all actions were successful.
        /// </summary>
        public OperationFailureState GetFailureState()
        {
            var last = _results.LastOrDefault();
            if (last == null || last.IsSuccess) return null;

            var compiledResult = _results.CompileFailResult();
            return OperationFailureState.FromResult(compiledResult);
        }

        /// <summary>
        /// Returns a value indicating whether the pipeline was an overall success, i.e., no exceptions occurred,
        /// and no <see cref="OperationResult"/> instances were returned by executed methods where <see cref="OperationResult.IsSuccess"/>
        /// was false.
        /// </summary>
        public bool IsSuccess()
            => _results.LastOrDefault()?.IsSuccess ?? true;

        internal OperationResult GetLastResult()
            => _results.Count > 0 ? _results[^1] : null;

        internal void PushResult(OperationResult result)
        {
            _results.Add(result);
            _failActions.Add(FailAction.Empty);
        }

        internal void PushFailActionResult(OperationResult result)
        {
            _failActionResults ??= [];
            _failActionResults.Add(result);
        }

        internal void PushFailAction(Func<Task> action)
            => PushFailAction(new FailAction(action));

        internal void PushFailAction(Func<OperationResult, Task> action)
            => PushFailAction(new FailAction(action));

        internal void PushFinalAction(Func<Task> final)
        {
            if (_finalAction != null) throw new InvalidOperationException("A final action has already been specified");
            _finalAction = final;
        }

        private void PushFailAction(FailAction failAction)
        {
            var index = _failActions.Count > 0 ? _failActions.Count - 1 : 0;
            _failActions[index] = failAction;
        }

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
}