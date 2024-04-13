*Nfinity.Extensions.Pipes* provides a mechanism for asynchronous pipeline processing in .NET, allowing multiple method calls to be combined into a single pipeline. This is usually provided by the `|` operator in many functional languages, but *Extensions.Pipes* provides a mechanism reminiscent of LINQ to achieve the same.

Using method pipelining maintains clean code principles, and massively reduces the amount of code required to reliably execute multiple methods in succession, where each is dependent on the success of its predecessor. This is not only true for more complex scenarios, but can help to simplify code in general.

This becomes even more evident when needing conditional code based on the individual results of multiple operations.

See more complete documentation with examples [at the GitHub repository](https://github.com/NfinitySoftware/extensions-pipes).