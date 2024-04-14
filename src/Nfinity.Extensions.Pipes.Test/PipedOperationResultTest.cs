using System.Net;

namespace Nfinity.Extensions.Pipes.Test
{
    [TestClass]
    public class PipedOperationResultTest
    {
        [TestMethod]
        public void CompileFailResult_NoResults()
        {
            var result = new PipedOperationResult();
            Assert.IsNull(result.CompileFailResult());
        }

        [TestMethod]
        public void CompileFailResult_NoneFailed()
        {
            var result = new PipedOperationResult();
            result.PushResult(OperationResult.Success());
            result.PushResult(OperationResult.Success());

            Assert.IsNull(result.CompileFailResult());
        }

        [TestMethod]
        public void CompileFailResult_OneTaskFailed()
        {
            var result = new PipedOperationResult();
            result.PushResult(OperationResult.Success());
            result.PushResult(GetFailedResult("failed"));
            
            var compiled = result.CompileFailResult();
            Assert.IsNotNull(compiled);
            Assert.AreEqual("failed", compiled.FailureReason);
            Assert.IsNotNull(compiled.Exception);
            Assert.IsFalse(compiled.IsSuccess);
            Assert.AreEqual(HttpStatusCode.InternalServerError, compiled.HttpStatusCode);
        }

        [TestMethod]
        public void CompileFailResult_MultipleTasksFailed()
        {
            var result = new PipedOperationResult();
            result.PushResult(GetFailedResult("failed1", HttpStatusCode.BadRequest));
            result.PushResult(GetFailedResult("failed2"));
            result.PushResult(GetFailedResult("failed3"));

            var compiled = result.CompileFailResult();
            Assert.IsNotNull(compiled);
            Assert.AreEqual("failed1", compiled.FailureReason, "Expected the first error reason");
            Assert.IsNotNull(compiled.Exception);
            Assert.AreEqual(3, ((AggregateException)compiled.Exception).InnerExceptions.Count);
            Assert.IsFalse(compiled.IsSuccess);
            Assert.AreEqual(HttpStatusCode.BadRequest, compiled.HttpStatusCode, "Expected the http status code of the first error");
        }

        [TestMethod]
        public void CompileFailResult_MultipleTasksFailed_SomeNull()
        {
            var result = new PipedOperationResult();
            result.PushResult(GetFailedResult("failed1", HttpStatusCode.BadRequest));
            result.PushResult(null);
            result.PushResult(GetFailedResult("failed3"));

            var compiled = result.CompileFailResult();
            Assert.IsNotNull(compiled);
            Assert.AreEqual("failed1", compiled.FailureReason, "Expected the first error reason");
            Assert.IsNotNull(compiled.Exception);
            Assert.AreEqual(2, ((AggregateException)compiled.Exception).InnerExceptions.Count, "Expected to ignore the null task");
            Assert.IsFalse(compiled.IsSuccess);
            Assert.AreEqual(HttpStatusCode.BadRequest, compiled.HttpStatusCode, "Expected the http status code of the first error");
        }

        private static OperationResult GetFailedResult(string message, HttpStatusCode httpStatusCode = HttpStatusCode.InternalServerError)
        {
            return OperationResult.Fail(new Exception(message), message, statusCode: httpStatusCode);
        }
    }
}
