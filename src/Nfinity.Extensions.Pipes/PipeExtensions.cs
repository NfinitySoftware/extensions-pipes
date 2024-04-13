using Nfinity.Extensions.Pipes.Extensions;

namespace Nfinity.Extensions.Pipes
{
    /// <summary>
    /// Pipeline extension methods.
    /// </summary>
    public static class PipeExtensions
    {
        /// <summary>
        /// An alternative method for starting a pipeline.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="next">The next method to execute.</param>
        public static async Task<PipedOperationResult> PipeAsync(this Func<Task<OperationResult>> first, Func<Task<OperationResult>> next)
        {
            var result = new PipedOperationResult();

            await AsyncPipe.ExecuteAsync(result, first);
            if (result.IsSuccess())
            {
                await AsyncPipe.ExecuteAsync(result, next);
            }

            return result;
        }

        /// <summary>
        /// Enqueues a method in the pipeline.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="next">The next method to execute.</param>
        public static async Task<PipedOperationResult> PipeAsync(this Task<PipedOperationResult> result, Func<Task<OperationResult>> next)
        {
            var pipedResult = await result;
            if (!pipedResult.IsSuccess()) return pipedResult;

            await AsyncPipe.ExecuteAsync(pipedResult, next);
            return pipedResult;
        }

        /// <summary>
        /// Enqueues a method in the pipeline.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="next">The next method to execute, which accepts the <see cref="OperationResult"/> instance returned from its predecessor.</param>
        public static async Task<PipedOperationResult> PipeAsync(this Task<PipedOperationResult> result, Func<OperationResult, Task<OperationResult>> next)
        {
            var pipedResult = await result;
            if (!pipedResult.IsSuccess()) return pipedResult;

            await AsyncPipe.ExecuteAsync(pipedResult, next);
            return pipedResult;
        }

        /// <summary>
        /// Enqueues a failure handler method to execute should the preceding method fail.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="onFail">The method to execute.</param>
        public static async Task<PipedOperationResult> OnFailAsync(this Task<PipedOperationResult> result, Func<Task> onFail)
        {
            var pipedResult = await result;
            
            pipedResult.PushFailAction(onFail);
            if (pipedResult.IsSuccess()) return pipedResult;
            
            await AsyncPipe.ExecuteFailActionAsync(pipedResult);
            return pipedResult;
        }

        /// <summary>
        /// Enqueues a failure handler method to execute should the preceding method fail.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="onFail">The method to execute, which accepts the <see cref="OperationResult"/> instance returned from its predecessor.</param>
        public static async Task<PipedOperationResult> OnFailAsync(this Task<PipedOperationResult> result, Func<OperationResult, Task> onFail)
        {
            var pipedResult = await result;

            pipedResult.PushFailAction(onFail);
            if (pipedResult.IsSuccess()) return pipedResult;

            await AsyncPipe.ExecuteFailActionAsync(pipedResult);
            return pipedResult;
        }

        /// <summary>
        /// Enqueues a method to execute once all other methods in the pipeline have completed.
        /// This method should be the last specified in the chain. This method can be called only once in the pipeline setup.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="final">The method to execute.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when a final action has already been specified in the pipeline.
        /// </exception>
        public static async Task<PipedOperationResult> Finally(this Task<PipedOperationResult> result, Func<Task> final)
        {
            var pipedResult = await result;
            
            pipedResult.PushFinalAction(final);
            await AsyncPipe.ExecuteFinallyAsync(pipedResult, final);
            return pipedResult;
        }

        internal static OperationResult CompileFailResult(this IList<OperationResult> results)
        {
            if (results.IsNullOrEmpty()) return null;

            var firstFailed = (OperationResult)null;
            var exceptions = new List<Exception>();
            var anyRetryable = false;

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

                if (!anyRetryable && result.IsRetryable)
                {
                    anyRetryable = true;
                }
            }

            if (firstFailed == null) return null;

            var aggregateException = new AggregateException(exceptions);

            var failureReason = firstFailed.FailureReason ?? exceptions[0].Message;
            var httpStatusCode = firstFailed.HttpStatusCode;

            return OperationResult.Fail(failureReason, aggregateException, isRetryable: anyRetryable, statusCode: httpStatusCode);
        }
    }
}
