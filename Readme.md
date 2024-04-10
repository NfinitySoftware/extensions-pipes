## Nfinity Extensions Pipes

- [What is Extensions.Pipes?](#what-is-extensionspipes)
- [Why use Extensions.Pipes](#why-use-extensionspipes)?
- [How can I use Extensions.Pipes](#using-extensionspipes)?
- [License](#license)

### What is Extensions.Pipes?
*Extensions.Pipes* provides a mechanism for asynchronous pipeline processing in .NET, allowing multiple method calls to be combined into a single pipeline. This is usually provided by the `|` operator in many functional languages, but *Extensions.Pipes* provides a mechanism reminiscent of LINQ to achieve the same.

#### Main features:
- Method calls can be chained. The invocation of each method is dependent on the success of its predecessor.
- Piped methods can be followed by piped failure actions, which are automatically invoked should its associated method fail.
- The behavior of the invocation of pipeline failure actions can be customised. Briefly, the options are: invoke only the last failure action (the default); invoke all specified failure actions up the stack.
- A `finally` action can be specified, completing a *try-catch-finally* construct in the pipeline, and which is always invoked whether pipeline actions succeed or fail.
- An aggregate result is made available as the result of the pipeline, and conditional code can be written based on its overall success or failure.

### Why use Extensions.Pipes?
- Massively reduce the amount of code required to reliably execute multiple methods in succession, where each is dependent on the success of its predecessor.
- Take advantage of a standardised way to aggregate the result of multiple, dependent operations, and act on them once all are complete.
- Maintain clean code principles, especially in complex scenario method chaining.

### Using Extensions.Pipes
> Notes:
>- Use of Extensions.Pipes is based on use of the `OperationResult` type. Each chained method should return `Task<OperationResult>`.
>  - It's recommended that chained methods do not throw exceptions, but rather return an `OperationResult` instance using `OperationResult.Fail(..)`.

#### Examples:
```csharp
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

    //Conditional code based on the aggregated result
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
```


### License
Nfinity.Extensions.Pipes is licensed under the [MIT](LICENSE.txt) license.