namespace Nfinity.Extensions.Pipes.Abstractions
{
    /// <summary>
    /// Abstraction representing the state of a completed pipeline.
    /// </summary>
    /// <typeparam name="T">The type of the result returned from each invoked action in the pipeline.</typeparam>
    public abstract class PipelineResult<T>
        where T : class
    {
        /// <summary>
        /// Gets a reference to a <see cref="PipedOperationOptions"/> instance, representing the
        /// options with which the pipeline was created.
        /// </summary>
        public PipedOperationOptions Options { get; }

        private readonly List<T> _results = [];
        /// <summary>
        /// Gets a reference to the list of pipeline results, in the order in which they were executed.
        /// </summary>
        public IReadOnlyList<T> Results => _results;

        private List<T> _failActionResults;
        /// <summary>
        /// Gets a reference to the list of results of all failure actions, in the order in which they were executed,
        /// if any were specified or run.
        /// </summary>
        public IReadOnlyList<T> FailActionResults => _failActionResults;

        /// <summary>
        /// Gets a reference to the result of the 'Finally' action, if one was specified.
        /// </summary>
        public T FinalResult { get; internal set; }

        private readonly List<FailAction> _failActions = [];
        internal IReadOnlyList<FailAction> FailActions => _failActions;

        private Func<Task> _finalAction;
        internal Func<Task> FinalAction => _finalAction;

        internal bool HasExecutedFailActions { get; set; }

        internal PipelineResult()
        {
            Options = new PipedOperationOptions();
        }

        internal PipelineResult(PipeFailureBehavior failureBehaviour)
        {
            Options = new PipedOperationOptions(failureBehaviour);
        }

        internal T GetLastResult()
            => _results.Count > 0 ? _results[^1] : null;

        internal void PushResult(T result)
        {
            _results.Add(result);
            _failActions.Add(FailAction.Empty);
        }

        internal void PushFailActionResult(T result)
        {
            _failActionResults ??= [];
            _failActionResults.Add(result);
        }

        internal void PushFailAction(Func<Task> action)
            => PushFailAction(new FailAction(action));

        internal void PushFinalAction(Func<Task> final)
        {
            if (_finalAction != null) throw new InvalidOperationException("A final action has already been specified");
            _finalAction = final;
        }

        internal void PushFailAction(FailAction failAction)
        {
            var index = _failActions.Count > 0 ? _failActions.Count - 1 : 0;
            _failActions[index] = failAction;
        }
    }
}
