using System.Net;

namespace Nfinity.Extensions.Pipes.Test
{
    [TestClass]
    public class PipeExtensionsTest
    {
        [TestMethod]
        public void CompileFailResult_NullCollection()
        {
            var tasks = (OperationResult[])null;
            Assert.IsNull(tasks.CompileFailResult());
        }

        [TestMethod]
        public void CompileFailResult_EmptyCollection()
        {
            var tasks = Array.Empty<OperationResult>();
            Assert.IsNull(tasks.CompileFailResult());
        }

        [TestMethod]
        public void CompileFailResult_NoneFailed()
        {
            var tasks = new[]
            {
                OperationResult.Success(),
                OperationResult.Success()
            };

            Assert.IsNull(tasks.CompileFailResult());
        }

        [TestMethod]
        public void CompileFailResult_OneTaskFailed()
        {
            var successResult = OperationResult.Success();
            var failResult = GetFailedResult("failed");
            
            var compiled = new[] { successResult, failResult }.CompileFailResult();
            Assert.IsNotNull(compiled);
            Assert.AreEqual("failed", compiled.FailureReason);
            Assert.IsNotNull(compiled.Exception);
            Assert.IsFalse(compiled.IsSuccess);
            Assert.AreEqual(HttpStatusCode.InternalServerError, compiled.HttpStatusCode);
        }

        [TestMethod]
        public void CompileFailResult_MultipleTasksFailed()
        {
            var failResult1 = GetFailedResult("failed1", HttpStatusCode.BadRequest);
            var failResult2 = GetFailedResult("failed2");
            var failResult3 = GetFailedResult("failed3");

            var compiled = new[] { failResult1, failResult2, failResult3 }.CompileFailResult();
            Assert.IsNotNull(compiled);
            Assert.AreEqual("failed1", compiled.FailureReason, "Expected the first error reason");
            Assert.IsNotNull(compiled.Exception);
            Assert.IsInstanceOfType<AggregateException>(compiled.Exception);
            Assert.AreEqual(3, ((AggregateException)compiled.Exception).InnerExceptions.Count);
            Assert.IsFalse(compiled.IsSuccess);
            Assert.AreEqual(HttpStatusCode.BadRequest, compiled.HttpStatusCode, "Expected the http status code of the first error");
        }

        [TestMethod]
        public void CompileFailResult_MultipleTasksFailed_SomeNull()
        {
            var failResult1 = GetFailedResult("failed1", HttpStatusCode.BadRequest);
            var failResult2 = (OperationResult)null;
            var failResult3 = GetFailedResult("failed3");

            var compiled = new[] { failResult1, failResult2, failResult3 }.CompileFailResult();
            Assert.IsNotNull(compiled);
            Assert.AreEqual("failed1", compiled.FailureReason, "Expected the first error reason");
            Assert.IsNotNull(compiled.Exception);
            Assert.IsInstanceOfType<AggregateException>(compiled.Exception);
            Assert.AreEqual(2, ((AggregateException)compiled.Exception).InnerExceptions.Count, "Expected to ignore the null task");
            Assert.IsFalse(compiled.IsSuccess);
            Assert.AreEqual(HttpStatusCode.BadRequest, compiled.HttpStatusCode, "Expected the http status code of the first error");
        }

        private static OperationResult GetFailedResult(string message, HttpStatusCode httpStatusCode = HttpStatusCode.InternalServerError)
        {
            return OperationResult.Fail(message, new Exception(message), statusCode: httpStatusCode);
        }
    }
}
