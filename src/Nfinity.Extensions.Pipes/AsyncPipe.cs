namespace Nfinity.Extensions.Pipes
{
    public static class AsyncPipe
    {
        private const string OperationFailedMessage = "Executing the operation in async pipe failed.";

        public static async Task<PipedOperationResult> Start(Func<Task<OperationResult>> first)
        {
            var result = new PipedOperationResult();
            await ExecuteAsync(result, first);
            return result;
        }

        public static async Task<PipedOperationResult> Start(Func<Task<OperationResult>> first, PipeFailureBehavior failureBehaviour)
        {
            var result = new PipedOperationResult(failureBehaviour);
            await ExecuteAsync(result, first);
            return result;
        }

        internal static async Task ExecuteAsync(PipedOperationResult result, Func<Task<OperationResult>> getAsyncOperation)
        {
            var (task, operationResult) = await ExecuteAsync(getAsyncOperation);
            result.PushResult(operationResult, task);
        }

        internal static async Task ExecuteFailActionAsync(PipedOperationResult result)
        {
            if (result.HasExecutedFailActions) return;

            var onlyFailLast = result.Options.ShouldOnlyFailLast;
            var lastIndex = result.FailActions.Count - 1;

            for (var i = lastIndex; i >= 0; i--)
            {
                var (task, operationResult) = await ExecuteAsync(result.FailActions[i]);
                result.PushFailActionResult(operationResult, task);

                if (i == lastIndex && onlyFailLast) break;
            }

            result.HasExecutedFailActions = true;
        }

        internal static async Task ExecuteFinallyAsync(PipedOperationResult result, Func<Task> final)
        {
            var (task, operationResult) = await ExecuteAsync(final);
            result.FinalResult = operationResult;
        }

        private static async Task<(Task<OperationResult>, OperationResult)> ExecuteAsync(Func<Task<OperationResult>> getAsyncOperation)
        {
            var task = (Task<OperationResult>)null;

            try
            {
                task = getAsyncOperation();
                var result = await task;
                return (task, result);
            }
            catch (Exception ex)
            {
                return (task, OperationResult.Fail(OperationFailedMessage, ex));
            }
        }

        private static async Task<(Task, OperationResult)> ExecuteAsync(Func<Task> getAsyncOperation)
        {
            var task = (Task)null;

            try
            {
                task = getAsyncOperation();
                await task;
                return (task, OperationResult.Success());
            }
            catch (Exception ex)
            {
                return (task, OperationResult.Fail(OperationFailedMessage, ex));
            }
        }
    }
}
