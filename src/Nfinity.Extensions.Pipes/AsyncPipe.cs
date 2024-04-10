using System.Threading.Tasks;

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

        internal static async Task ExecuteFailActionAsync(PipedOperationResult result)
        {
            if (result.HasExecutedFailActions) return;

            var onlyFailLast = result.Options.ShouldOnlyFailLast;
            var lastIndex = result.FailActions.Count - 1;
            var resultStack = new Stack<OperationResult>(result.Results);

            for (var i = lastIndex; i >= 0; i--)
            {
                var failAction = result.FailActions[i];
                if (failAction.HasRun) continue;

                if (failAction.Action != null)
                {
                    var failOperationResult = await ExecuteAsync(failAction.Action);
                    result.PushFailActionResult(failOperationResult);
                }
                else if (failAction.ActionWithResultArg != null)
                {
                    resultStack.TryPop(out var operationResult);
                    var failOperationResult = await ExecuteAsync(failAction.ActionWithResultArg, operationResult);
                    result.PushFailActionResult(failOperationResult);
                }

                failAction.HasRun = true;

                if (i == lastIndex && onlyFailLast) break;
            }

            result.HasExecutedFailActions = true;
        }

        internal static async Task ExecuteFinallyAsync(PipedOperationResult result, Func<Task> final)
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
                return OperationResult.Fail(OperationFailedMessage, ex);
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
                return OperationResult.Fail(OperationFailedMessage, ex);
            }
        }

        private static async Task<OperationResult> ExecuteAsync(Func<Task> asyncOperation)
        {
            try
            {
                await asyncOperation();
                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                return OperationResult.Fail(OperationFailedMessage, ex);
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
                return OperationResult.Fail(OperationFailedMessage, ex);
            }
        }
    }
}
