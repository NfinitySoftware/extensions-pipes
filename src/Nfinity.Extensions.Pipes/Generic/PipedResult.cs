namespace Nfinity.Extensions.Pipes.Generic
{
    public sealed class PipedResult
    {
        internal readonly static PipedResult Success = new(true);
        internal readonly static PipedResult Fail = new(false);

        public bool IsSuccess { get; }

        private PipedResult(bool isSuccess)
        {
            IsSuccess = isSuccess;
        }
    }
}
