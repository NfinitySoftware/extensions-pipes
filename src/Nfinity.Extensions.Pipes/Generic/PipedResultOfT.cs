namespace Nfinity.Extensions.Pipes.Generic
{
    public sealed class PipedResult<T> : PipedResult
    {
        private readonly static PipedResult<T> _fail = new(false);

        public T Data { get; }

        private PipedResult(bool isSuccess, T data = default, Exception exception = null)
            : base(isSuccess)
        {
            Data = data;
        }

        new internal static PipedResult<T> Success(T data)
            => new(true, data);

        new internal static PipedResult<T> Fail()
            => _fail;

        new internal static PipedResult<T> Fail(Exception exception)
            => new(false, exception: exception);
    }
}
