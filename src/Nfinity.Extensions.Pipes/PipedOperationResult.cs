﻿using Nfinity.Extensions.Pipes.Extensions;

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
        /// Gets a reference to the list of pipeline results, in the order in which they were executed.
        /// </summary>
        public IReadOnlyList<OperationResult> Results => _results;

        private List<OperationResult> _failActionResults;
        /// <summary>
        /// Gets a reference to the list of results of all failure actions, in the order in which they were executed,
        /// if any were specified or run.
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
            if (IsSuccess()) return null;

            var compiledResult = CompileFailResult();
            return OperationFailureState.FromResult(compiledResult);
        }

        /// <summary>
        /// Returns a value indicating whether the pipeline was an overall success, i.e., no exceptions occurred,
        /// and no <see cref="OperationResult"/> instances were returned by executed methods where <see cref="OperationResult.IsSuccess"/>
        /// was false.
        /// </summary>
        public bool IsSuccess()
            => _results.LastOrDefault().IsNullOrSuccess() && FinalResult.IsNullOrSuccess();

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

        internal OperationResult CompileFailResult()
        {
            if (_results.IsNullOrEmpty()) return null;

            var exceptions = new List<Exception>();

            var haveFailures = CompileFailures(_results, exceptions, out var isAnyOpRetryable, out var firstFailed);
            var haveFailedFailureActions = CompileFailures(_failActionResults, exceptions, out var isAnyFailureActionRetryable, out var firstFailureActionResult);
            var haveFailedFinal = CompileFailures([FinalResult], exceptions, out var isFinalActionRetryable, out var failedFinal);

            if (!haveFailures && !haveFailedFailureActions && !haveFailedFinal) return null;

            var first = firstFailed ?? firstFailureActionResult ?? failedFinal;
            var anyRetryable = isAnyOpRetryable || isAnyFailureActionRetryable || isFinalActionRetryable;

            var aggregateException = exceptions.Count == 0 ? null : new AggregateException(exceptions);
            var failureReason = first.FailureReason ?? exceptions.FirstOrDefault()?.Message;
            var httpStatusCode = first.HttpStatusCode;

            return OperationResult.Fail(aggregateException, failureReason, isRetryable: anyRetryable, statusCode: httpStatusCode);
        }

        private static bool CompileFailures(IReadOnlyList<OperationResult> results, List<Exception> exceptions, 
            out bool isAnyOperationRetryable, out OperationResult firstFailedResult)
        {
            if (results.IsNullOrEmpty())
            {
                isAnyOperationRetryable = false;
                firstFailedResult = null;
                return false;
            }

            var firstFailed = (OperationResult)null;
            var isAnyRetryable = false;

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

                if (!isAnyRetryable && result.IsRetryable)
                {
                    isAnyRetryable = true;
                }
            }

            firstFailedResult = firstFailed;
            isAnyOperationRetryable = isAnyRetryable;
            return firstFailed != null;
        }
    }
}