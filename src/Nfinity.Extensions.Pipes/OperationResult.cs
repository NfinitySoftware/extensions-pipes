using Nfinity.Extensions.Pipes.Extensions;
using System.Net;

namespace Nfinity.Extensions.Pipes
{
    public class OperationResult
    {
        private readonly static OperationResult _success = new() { IsSuccess = true, HttpStatusCode = HttpStatusCode.OK };

        public required bool IsSuccess { get; init; }
        public string FailureReason { get; init; }
        public Exception Exception { get; init; }
        public HttpStatusCode HttpStatusCode { get; init; }
        public bool IsRetryable { get; init; }

        public static OperationResult Success()
            => _success;

        public static OperationResult Fail(string reason, Exception exception = null, bool isRetryable = true, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
            => new() { IsSuccess = false, FailureReason = reason, Exception = exception, IsRetryable = isRetryable, HttpStatusCode = statusCode };

        public static OperationResult Resolve(params Task<OperationResult>[] tasks)
        {
            if (tasks.IsNullOrEmpty()) return null;

            var failed = tasks.FirstOrDefault(t => t != null && (!t.IsCompletedSuccessfully || t.IsFaulted || !t.Result.IsSuccess));
            if (failed == null) return Success();

            var message = failed.Result.FailureReason ?? failed.Result.Exception?.Message;
            var exception = failed.Result.Exception ?? failed.Exception;

            return Fail(message, exception, statusCode: failed.Result.HttpStatusCode);
        }
    }
}
