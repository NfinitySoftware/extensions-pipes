namespace Nfinity.Extensions.Pipes.Generic
{
    /// <summary>
    /// A mechanism used to start an asynchronous pipeline.
    /// </summary>
    public static class AsyncPipe
    {
        /// <summary>
        /// Starts an asynchronous pipeline.
        /// </summary>
        /// <param name="first">The first method to execute.</param>
        public static Task<PipedResult<T>> Start<T>(Func<Task<T>> first)
            => ExecuteAsync(first);


        
        /// <summary>
        /// Starts an asynchronous pipeline with the given failure behavior.
        /// </summary>
        /// <param name="first">The first method to execute.</param>
        /// <param name="failureBehaviour">The way in which to execute failure actions in the pipeline.</param>
        public static async Task<PipedResult> Start(Func<Task<PipedResult>> first, PipeFailureBehavior failureBehaviour)
        {
            var result = new PipedResult(failureBehaviour);
            await ExecuteAsync(result, first);
            return result;
        }

        /// <summary>
        /// Starts an asynchronous pipeline.
        /// </summary>
        /// <param name="first">The first method to execute.</param>
        public static async Task<PipedTaskResult> Start(Func<Task> first)
        {
            var result = new PipedTaskResult();
            await ExecuteAsync(result, first);
            return result;
        }

        /// <summary>
        /// Starts an asynchronous pipeline with the given failure behavior.
        /// </summary>
        /// <param name="first">The first method to execute.</param>
        /// <param name="failureBehaviour">The way in which to execute failure actions in the pipeline.</param>
        public static async Task<PipedTaskResult> Start(Func<Task> first, PipeFailureBehavior failureBehaviour)
        {
            var result = new PipedTaskResult(failureBehaviour);
            await ExecuteAsync(result, first);
            return result;
        }

        internal static async Task<T> ExecuteAsync<T>(PipedResult result, Func<Task<T>> asyncOperation)
        {
            var operationResult = await ExecuteAsync(asyncOperation);
            
        }

        internal static async Task ExecuteAsync(PipedResult result, Func<PipedResult, Task<PipedResult>> asyncOperation)
        {
            var lastResult = result.GetLastResult();
            var operationResult = await ExecuteAsync(asyncOperation, lastResult);
            result.PushResult(operationResult);
        }

        internal static async Task ExecuteAsync(PipedTaskResult result, Func<Task> asyncOperation)
        {
            var taskResult = await ExecuteAsync(asyncOperation);
            result.PushResult(taskResult);
        }

        internal static async Task ExecuteFailActionAsync(PipedResult result)
        {
            if (result.HasExecutedFailActions) return;

            var onlyFailLast = result.Options.ShouldOnlyFailLast;
            var lastIndex = result.FailActions.Count - 1;
            var resultStack = new Stack<PipedResult>(result.Results);

            for (var i = lastIndex; i >= 0; i--)
            {
                var failAction = result.FailActions[i];
                if (failAction.IsEmpty) continue;

                if (failAction.Action != null)
                {
                    var failOperationResult = await ExecuteWithOperationResultAsync(failAction.Action);
                    result.PushFailActionResult(failOperationResult);
                }
                else if (failAction.ActionWithResultArg != null)
                {
                    resultStack.TryPop(out var operationResult);
                    var failOperationResult = await ExecuteAsync(failAction.ActionWithResultArg, operationResult);
                    result.PushFailActionResult(failOperationResult);
                }

                if (i == lastIndex && onlyFailLast) break;
            }

            result.HasExecutedFailActions = true;
        }

        internal static async Task ExecuteFailActionAsync(PipedTaskResult result)
        {
            if (result.HasExecutedFailActions) return;

            var onlyFailLast = result.Options.ShouldOnlyFailLast;
            var lastIndex = result.FailActions.Count - 1;

            for (var i = lastIndex; i >= 0; i--)
            {
                var failAction = result.FailActions[i];
                if (failAction.IsEmpty) continue;

                if (failAction.Action != null)
                {
                    var failOperationResult = await ExecuteAsync(failAction.Action);
                    result.PushFailActionResult(failOperationResult);
                }

                if (i == lastIndex && onlyFailLast) break;
            }

            result.HasExecutedFailActions = true;
        }

        internal static async Task ExecuteFinallyAsync(PipedResult result, Func<Task> final)
        {
            result.FinalResult = await ExecuteWithOperationResultAsync(final);
        }

        internal static async Task ExecuteFinallyAsync(PipedTaskResult result, Func<Task> final)
        {
            result.FinalResult = await ExecuteAsync(final);
        }

        private static async Task<PipedResult> ExecuteAsync(Func<Task<PipedResult>> asyncOperation)
        {
            try
            {
                var result = await asyncOperation();
                return result;
            }
            catch (Exception ex)
            {
                return PipedResult.Fail(ex);
            }
        }

        private static async Task<PipedResult> ExecuteAsync(Func<PipedResult, Task<PipedResult>> asyncOperation, PipedResult lastResult)
        {
            try
            {
                var result = await asyncOperation(lastResult);
                return result;
            }
            catch (Exception ex)
            {
                return PipedResult.Fail(ex);
            }
        }

        private static async Task<PipedResult> ExecuteWithOperationResultAsync(Func<Task> asyncOperation)
        {
            try
            {
                await asyncOperation();
                return PipedResult.Success();
            }
            catch (Exception ex)
            {
                return PipedResult.Fail(ex);
            }
        }

        private static async Task<PipedResult> ExecuteAsync(Func<PipedResult, Task> asyncOperation, PipedResult lastResult)
        {
            try
            {
                await asyncOperation(lastResult);
                return PipedResult.Success();
            }
            catch (Exception ex)
            {
                return PipedResult.Fail(ex);
            }
        }

        private static async Task<PipedResult<T>> ExecuteAsync<T>(Func<Task<T>> asyncOperation)
        {
            try
            {
                var data = await asyncOperation();
                return PipedResult<T>.Success(data);
            }
            catch (Exception ex)
            {
                return PipedResult<T>.Fail(ex);
            }
        }
    }
}
