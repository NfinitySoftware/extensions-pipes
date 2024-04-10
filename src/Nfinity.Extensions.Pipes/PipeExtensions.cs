using Nfinity.Extensions.Pipes.Extensions;

namespace Nfinity.Extensions.Pipes
{
    public static class PipeExtensions
    {
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

        public static async Task<PipedOperationResult> PipeAsync(this Task<PipedOperationResult> result, Func<Task<OperationResult>> next)
        {
            var pipedResult = await result;
            if (!pipedResult.IsSuccess()) return pipedResult;

            await AsyncPipe.ExecuteAsync(pipedResult, next);
            return pipedResult;
        }

        public static async Task<PipedOperationResult> PipeAsync(this Task<PipedOperationResult> result, Func<OperationResult, Task<OperationResult>> next)
        {
            var pipedResult = await result;
            if (!pipedResult.IsSuccess()) return pipedResult;

            await AsyncPipe.ExecuteAsync(pipedResult, next);
            return pipedResult;
        }

        public static async Task<PipedOperationResult> OnFailAsync(this Task<PipedOperationResult> result, Func<Task> onFail)
        {
            var pipedResult = await result;
            
            pipedResult.PushFailAction(onFail);
            if (pipedResult.IsSuccess()) return pipedResult;
            
            await AsyncPipe.ExecuteFailActionAsync(pipedResult);
            return pipedResult;
        }

        public static async Task<PipedOperationResult> OnFailAsync(this Task<PipedOperationResult> result, Func<OperationResult, Task> onFail)
        {
            var pipedResult = await result;

            pipedResult.PushFailAction(onFail);
            if (pipedResult.IsSuccess()) return pipedResult;

            await AsyncPipe.ExecuteFailActionAsync(pipedResult);
            return pipedResult;
        }

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

            for (var i = 0; i < results.Count; i++)
            {
                var result = results[i];
                if (result == null || result.IsSuccess) continue;

                firstFailed ??= result;

                var exception = result.Exception;
                if (exception != null) exceptions.Add(exception);
            }

            if (firstFailed == null) return null;

            var aggregateException = new AggregateException(exceptions);

            var failureReason = firstFailed.FailureReason ?? exceptions[0].Message;
            var httpStatusCode = firstFailed.HttpStatusCode;

            return OperationResult.Fail(failureReason, aggregateException, statusCode: httpStatusCode);
        }
    }
}
