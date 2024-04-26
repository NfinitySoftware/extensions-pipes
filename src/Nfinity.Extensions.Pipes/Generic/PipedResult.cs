using System.Reflection;

namespace Nfinity.Extensions.Pipes.Generic
{
    public class PipedResult
    {
        private readonly static PipedResult _fail = new(false);
        internal readonly static PipedResult Success = new(true);

        private readonly List<FailAction> _failActions = [];
        internal IReadOnlyList<FailAction> FailActions => _failActions;

        public bool IsSuccess { get; internal set; }
        public Exception Exception { get; internal set; }

        internal PipedResult()
        {
        }

        internal PipedResult(bool isSuccess, Exception exception = null)
        {
            IsSuccess = isSuccess;
            Exception = exception;
        }

        internal static PipedResult Fail()
            => _fail;

        internal static PipedResult Fail(Exception exception)
            => new(false, exception);

        internal void EnqueueFailAction(MethodInfo method, object argument = null)
            => _failActions.Add(new FailAction(method, argument));
    }
}
