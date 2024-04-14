namespace Nfinity.Extensions.Pipes.Extensions
{
    internal static class ResultExtensions
    {
        public static bool IsNullOrSuccess(this OperationResult result)
            => result == null || result.IsSuccess;

        public static bool IsNullOrSuccess(this TaskResult result)
            => result == null || result.IsSuccess;
    }
}
