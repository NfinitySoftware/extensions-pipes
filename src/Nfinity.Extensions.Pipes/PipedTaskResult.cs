using Nfinity.Extensions.Pipes.Extensions;

namespace Nfinity.Extensions.Pipes
{
    /// <summary>
    /// Represents the overall result of an asynchronous pipeline.
    /// </summary>
    public class PipedTaskResult
    {
        /// <summary>
        /// Gets a reference to a <see cref="PipedOperationOptions"/> instance, representing the
        /// options with which the pipeline was created.
        /// </summary>
        public PipedOperationOptions Options { get; }

        private readonly List<TaskResult> _results = [];
        /// <summary>
        /// Gets a reference to the list of pipeline results, in the order in which they were executed.
        /// </summary>
        public IReadOnlyList<TaskResult> Results => _results;

        private List<TaskResult> _failActionResults;
        /// <summary>
        /// Gets a reference to the list of results of all failure actions, in the order in which they were executed,
        /// if any were specified or run.
        /// </summary>
        public IReadOnlyList<TaskResult> FailActionResults => _failActionResults;

        /// <summary>
        /// Gets a reference to the result of the 'Finally' action, if one was specified.
        /// </summary>
        public TaskResult FinalResult { get; internal set; }

        private readonly List<FailAction> _failActions = [];
        internal IReadOnlyList<FailAction> FailActions => _failActions;

        private Func<Task> _finalAction;
        internal Func<Task> FinalAction => _finalAction;

        internal bool HasExecutedFailActions { get; set; }

        internal PipedTaskResult()
        {
            Options = new PipedOperationOptions();
        }

        internal PipedTaskResult(PipeFailureBehavior failureBehaviour)
        {
            Options = new PipedOperationOptions(failureBehaviour);
        }

        /// <summary>
        /// Compiles the results of all failed pipeline actions and returns a <see cref="TaskFailureState"/> instance representing the result.
        /// Returns null if no actions were run, or if all actions were successful.
        /// </summary>
        public TaskFailureState GetFailureState()
        {
            if (IsSuccess()) return null;

            var compiledResult = CompileFailResult();
            return TaskFailureState.FromResult(compiledResult);
        }

        /// <summary>
        /// Returns a value indicating whether the pipeline was an overall success, i.e., no exceptions occurred,
        /// and no <see cref="TaskResult"/> instances were returned by executed methods where <see cref="TaskResult.IsSuccess"/>
        /// was false.
        /// </summary>
        public bool IsSuccess()
            => _results.LastOrDefault().IsNullOrSuccess() && FinalResult.IsNullOrSuccess();

        internal TaskResult GetLastResult()
            => _results.Count > 0 ? _results[^1] : null;

        internal void PushResult(TaskResult result)
        {
            _results.Add(result);
            _failActions.Add(FailAction.Empty);
        }

        internal void PushFailActionResult(TaskResult result)
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

        private void PushFailAction(FailAction failAction)
        {
            var index = _failActions.Count > 0 ? _failActions.Count - 1 : 0;
            _failActions[index] = failAction;
        }

        internal TaskResult CompileFailResult()
        {
            if (_results.IsNullOrEmpty()) return null;

            var exceptions = new List<Exception>();

            var haveFailures = CompileFailures(_results, exceptions);
            var haveFailedFailureActions = CompileFailures(_failActionResults, exceptions);
            var haveFailedFinal = CompileFailures([FinalResult], exceptions);

            if (!haveFailures && !haveFailedFailureActions && !haveFailedFinal) return null;

            var aggregateException = exceptions.Count == 0 ? null : new AggregateException(exceptions);

            return TaskResult.Fail(aggregateException);
        }

        private static bool CompileFailures(IReadOnlyList<TaskResult> results, List<Exception> exceptions)
        {
            if (results.IsNullOrEmpty()) return false;

            var firstFailed = (TaskResult)null;
            for (var i = 0; i < results.Count; i++)
            {
                var result = results[i];
                if (result == null || result.IsSuccess) continue;

                firstFailed ??= result;

                var exception = result.Exception;
                if (exception != null)
                {
                    exceptions.Add(exception);
                }
            }

            return firstFailed != null;
        }
    }
}
