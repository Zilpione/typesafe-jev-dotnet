# TypeSafe.Jev for .NET

An independent, minimal .NET client for the [TypeSafe System One API](https://docs.typesafe.ai/api). This project is not affiliated with TypeSafe AI.

```csharp
using TypeSafe.Jev;

using var http = new HttpClient();
var client = new TypeSafeClient(http, apiKey);
using var result = await client.EvaluateAsync("Electricity invoice",
    new Dictionary<string, object>
    {
        ["is_bill"] = new { type = "noul", instructions = "Is this a utility bill?" }
    });
var probability = result.RootElement.GetProperty("answers").GetProperty("is_bill").GetProperty("noul").GetDouble();
```

Pass one text or JSON state and any named `noul`, `choice`, or `score` questions. The client returns the raw JSON response. The caller owns `HttpClient` and disposes the returned `JsonDocument`. Keep the TypeSafe API key in your application's secret configuration; never commit it.

Build and test:

```sh
dotnet test tests/TypeSafe.Jev.Tests/TypeSafe.Jev.Tests.csproj
dotnet pack src/TypeSafe.Jev/TypeSafe.Jev.csproj -c Release
```

The package targets .NET 6. Publishing source on GitHub does not publish a package to NuGet.org.
