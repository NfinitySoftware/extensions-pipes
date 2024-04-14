namespace Nfinity.Extensions.Pipes
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
        public static async Task<PipedOperationResult> Start(Func<Task<OperationResult>> first)
        {
            var result = new PipedOperationResult();
            await ExecuteAsync(result, first);
            return result;
        }

        /// <summary>
        /// Starts an asynchronous pipeline with the given failure behavior.
        /// </summary>
        /// <param name="first">The first method to execute.</param>
        /// <param name="failureBehaviour">The way in which to execute failure actions in the pipeline.</param>
        public static async Task<PipedOperationResult> Start(Func<Task<OperationResult>> first, PipeFailureBehavior failureBehaviour)
        {
            var result = new PipedOperationResult(failureBehaviour);
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

        internal static async Task ExecuteAsync(PipedOperationResult result, Func<Task<OperationResult>> asyncOperation)
        {
            var operationResult = await ExecuteAsync(asyncOperation);
            result.PushResult(operationResult);
        }

        internal static async Task ExecuteAsync(PipedOperationResult result, Func<OperationResult, Task<OperationResult>> asyncOperation)
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

        internal static async Task ExecuteFailActionAsync(PipedOperationResult result)
        {
            if (result.HasExecutedFailActions) return;

            var onlyFailLast = result.Options.ShouldOnlyFailLast;
            var lastIndex = result.FailActions.Count - 1;
            var resultStack = new Stack<OperationResult>(result.Results);

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

        internal static async Task ExecuteFinallyAsync(PipedOperationResult result, Func<Task> final)
        {
            result.FinalResult = await ExecuteWithOperationResultAsync(final);
        }

        internal static async Task ExecuteFinallyAsync(PipedTaskResult result, Func<Task> final)
        {
            result.FinalResult = await ExecuteAsync(final);
        }

        private static async Task<OperationResult> ExecuteAsync(Func<Task<OperationResult>> asyncOperation)
        {
            try
            {
                var result = await asyncOperation();
                return result;
            }
            catch (Exception ex)
            {
                return OperationResult.Fail(ex);
            }
        }

        private static async Task<OperationResult> ExecuteAsync(Func<OperationResult, Task<OperationResult>> asyncOperation, OperationResult lastResult)
        {
            try
            {
                var result = await asyncOperation(lastResult);
                return result;
            }
            catch (Exception ex)
            {
                return OperationResult.Fail(ex);
            }
        }

        private static async Task<OperationResult> ExecuteWithOperationResultAsync(Func<Task> asyncOperation)
        {
            try
            {
                await asyncOperation();
                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                return OperationResult.Fail(ex);
            }
        }

        private static async Task<OperationResult> ExecuteAsync(Func<OperationResult, Task> asyncOperation, OperationResult lastResult)
        {
            try
            {
                await asyncOperation(lastResult);
                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                return OperationResult.Fail(ex);
            }
        }

        private static async Task<TaskResult> ExecuteAsync(Func<Task> asyncOperation)
        {
            try
            {
                await asyncOperation();
                return TaskResult.Success();
            }
            catch (Exception ex)
            {
                return TaskResult.Fail(ex);
            }
        }
    }
}
