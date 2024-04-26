using System.Reflection;

namespace Nfinity.Extensions.Pipes.Generic
{
    internal class FailAction
    {
        public readonly static FailAction Empty = new();

        public bool IsEmpty { get; }
        public MethodInfo Method { get; init; }
        public object Argument { get; init; }

        private FailAction()
        {
            IsEmpty = true;
        }

        public FailAction(MethodInfo method, object argument = null)
        {
            Method = method;
            Argument = argument;
        }
    }
}
