using Nfinity.Extensions.Pipes.Abstractions;
using Nfinity.Extensions.Pipes.Extensions;

namespace Nfinity.Extensions.Pipes
{
    /// <summary>
    /// Represents the overall result of an asynchronous pipeline.
    /// </summary>
    public class PipedTaskResult : PipelineResult<TaskResult>
    {
        internal PipedTaskResult()
        {
        }

        internal PipedTaskResult(PipeFailureBehavior failureBehaviour)
            : base(failureBehaviour)
        {
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
            => GetLastResult().IsNullOrSuccess() && FinalResult.IsNullOrSuccess();

        internal TaskResult CompileFailResult()
        {
            var results = Results;
            if (results.IsNullOrEmpty()) return null;

            var exceptions = new List<Exception>();

            var haveFailures = CompileFailures(results, exceptions);
            var haveFailedFailureActions = CompileFailures(FailActionResults, exceptions);
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
