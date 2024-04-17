namespace Nfinity.Extensions.Pipes.Generic
{
    public sealed class PipedResult<T>
    {
        public T Data { get; init; }
        public bool IsSuccess { get; init; }
    }
}
