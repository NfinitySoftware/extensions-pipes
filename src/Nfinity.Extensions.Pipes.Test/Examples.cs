using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nfinity.Extensions.Pipes.Test
{
    internal class Examples
    {
        public async Task ExecutePipelineAsync(string userName, string firstName, string lastName, string email)
        {
            //PipeFailureBehavior.FailAll will execute all the specified failure actions, such that if 
            //SetUserSecurityAsync fails, all failure actions up the stack will be called.
            //If the same action failed, and PipeFailureBehavior.FailLastOnly was specified, only 
            //DeleteUserSecurityAsync would be called.

            var result = await AsyncPipe
                .Start(() => CreateUserAsync(userName, firstName, lastName, email), PipeFailureBehavior.FailAll)
                .OnFailAsync(() => DeleteUserAsync(userName))
                .PipeAsync(() => SetUserSecurityAsync(userName, firstName, lastName, email))
                .OnFailAsync(() => DeleteUserSecurityAsync(userName))
                .Finally(() => CleanupTemporaryState(userName, firstName, lastName, email));

            //It's also possible to specify one failure action for a pipeline, which will be called if any action fails.
            result = await AsyncPipe
                .Start(() => CreateUserAsync(userName, firstName, lastName, email), PipeFailureBehavior.FailAll)
                .PipeAsync(() => SetUserSecurityAsync(userName, firstName, lastName, email))
                .OnFailAsync(() => DeleteUserSecurityAsync(userName))
                .Finally(() => CleanupTemporaryState(userName, firstName, lastName, email));

            //And it doesn't matter in which order Finally is called. This is ok.
            result = await AsyncPipe
                .Start(() => CreateUserAsync(userName, firstName, lastName, email), PipeFailureBehavior.FailAll)
                .Finally(() => CleanupTemporaryState(userName, firstName, lastName, email)) //Finally can be out of order
                .PipeAsync(() => SetUserSecurityAsync(userName, firstName, lastName, email))
                .OnFailAsync(() => DeleteUserSecurityAsync(userName));

            //Could also declare the first action outside the pipe and use the extension methods to create the pipeline.
            var createUserAction = () => CreateUserAsync(userName, firstName, lastName, email);
            result = await createUserAction
                .PipeAsync(() => SetUserSecurityAsync(userName, firstName, lastName, email))
                .OnFailAsync(() => DeleteUserSecurityAsync(userName));

            //BUT THIS WILL NOT WORK due to how async methods are immediately invoked
            var createUserTask = CreateUserAsync(userName, firstName, lastName, email);
            result = await createUserAction
                .PipeAsync(() => SetUserSecurityAsync(userName, firstName, lastName, email))
                .OnFailAsync(() => DeleteUserSecurityAsync(userName));

            if (!result.IsSuccess())
            {
                var failureState = result.GetFailureState();
                if (failureState.HttpStatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    //do something when a user already exists by the given username
                }
            }
        }

        private Task<OperationResult> CreateUserAsync(string userName, string firstName, string lastName, string email)
            => Task.FromResult(OperationResult.Success());

        private Task<OperationResult> SetUserSecurityAsync(string userName, string firstName, string lastName, string email)
            => Task.FromResult(OperationResult.Fail("Saving user security failed."));

        private Task<OperationResult> CleanupTemporaryState(string userName, string firstName, string lastName, string email)
            => Task.FromResult(OperationResult.Success());

        private Task<OperationResult> DeleteUserAsync(string userName)
            => Task.FromResult(OperationResult.Success());

        private Task<OperationResult> DeleteUserSecurityAsync(string userName)
            => Task.FromResult(OperationResult.Success());
    }
}
