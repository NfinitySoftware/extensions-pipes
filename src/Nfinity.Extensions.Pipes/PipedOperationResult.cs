using Nfinity.Extensions.Pipes.Abstractions;
using Nfinity.Extensions.Pipes.Extensions;

namespace Nfinity.Extensions.Pipes
{
    /// <summary>
    /// Represents the overall result of an asynchronous pipeline.
    /// </summary>
    public sealed class PipedOperationResult : PipelineResult<OperationResult>
    {
        internal PipedOperationResult()
        {
        }

        internal PipedOperationResult(PipeFailureBehavior failureBehaviour)
            : base(failureBehaviour)
        {
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
            => GetLastResult().IsNullOrSuccess() && FinalResult.IsNullOrSuccess();

        internal void PushFailAction(Func<OperationResult, Task> action)
            => PushFailAction(new FailAction(action));

        internal OperationResult CompileFailResult()
        {
            var results = Results;
            if (results.IsNullOrEmpty()) return null;

            var exceptions = new List<Exception>();

            var haveFailures = CompileFailures(results, exceptions, out var isAnyOpRetryable, out var firstFailed);
            var haveFailedFailureActions = CompileFailures(FailActionResults, exceptions, out var isAnyFailureActionRetryable, out var firstFailureActionResult);
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