# Zilpio.TypeSafe.Jev .NET Wrapper

An independent .NET wrapper for [Jev / TypeSafe System One](https://docs.typesafe.ai/api). It is not an official TypeSafe AI SDK.

Jev evaluates text against typed questions. This wrapper exposes `IJevService` with three methods: `Noul` for a yes/no probability, `Choice` for one option from a list, and `Score` for a position across ordered levels. Each method returns a typed result.

## Setup

Install the package from NuGet in Visual Studio's NuGet Package Manager, or run one of these commands from your project directory:

```sh
dotnet add package Zilpio.TypeSafe.Jev
```

In Visual Studio's Package Manager Console, run:

```powershell
Install-Package Zilpio.TypeSafe.Jev
```

The package supports .NET 6 and later, plus .NET Standard 2.0 consumers. This makes it usable from .NET Framework 4.6.1 and later; Microsoft recommends .NET Framework 4.7.2 or later for .NET Standard 2.0 libraries. Get a TypeSafe API key from the [TypeSafe dashboard](https://console.typesafe.ai/). Your application decides how to obtain the key; do not commit a real key.

Register the typed client once in `Program.cs`, passing the **actual API key value** in `apiKey`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using TypeSafe.Jev;

builder.Services.AddJevService(apiKey);
builder.Services.AddScoped<MessageAnalyzer>();
```

`"TypeSafe:ApiKey"` is a possible configuration *key name*, not the API key value. Passing that literal string would send it as a Bearer token. `AddJevService` rejects a null, empty, or whitespace API key immediately with `ArgumentException`; a nonempty but invalid key is rejected by TypeSafe when you call the API.

`AddJevService` registers `IJevService` as a **transient typed HTTP client** through `IHttpClientFactory`. The factory manages HTTP handlers; do not also register `IJevService` as scoped or singleton. Inject it into your controller or scoped/transient application service:

```csharp
public sealed class MessageAnalyzer
{
    private readonly IJevService _jev;

    public MessageAnalyzer(IJevService jev) => _jev = jev;

    public async Task<double> UrgencyAsync(string message, CancellationToken cancellationToken)
    {
        var answer = await _jev.Noul(
            new NoulRequest(message, "Does this message express urgency?"),
            cancellationToken);
        return answer.Probability;
    }
}
```

The following examples use `_jev` inside a class that receives `IJevService` through its constructor. Each method accepts an optional `CancellationToken`.

## Noul: yes/no probability

```csharp
var answer = await _jev.Noul(new NoulRequest(
    State: "Please call me back today; this is urgent.",
    Instructions: "Does the message express urgency?"));

double probabilityOfYes = answer.Probability; // 0 to 1
bool isUrgent = probabilityOfYes >= 0.8;        // choose your own threshold
```

`Probability` is the likelihood of **yes**, not a Boolean. Add `new NoulCriteria("What yes means", "What no means")` as the third argument when the boundary needs clarification.

## Choice: pick one option

```csharp
var answer = await _jev.Choice(new ChoiceRequest(
    State: "The app closes when I open settings.",
    Instructions: "Which team should handle this message?",
    Options: new List<ChoiceOption>
    {
        new("support", "Technical problems and bugs"),
        new("sales", "Purchasing and plans"),
        new("other", "None of these teams")
    }));

string selectedKey = answer.Value;
double confidence = answer.Confidence;
List<JevProbability> probabilities = answer.Probabilities;
```

Each `ChoiceOption` has a unique key and a description. `Value` is the selected key; `Probabilities` contains a `Key` and `Probability` for every option. Choice accepts 2 to 255 options.

## Score: rate ordered levels

```csharp
var answer = await _jev.Score(new ScoreRequest(
    State: "The issue affects most users and has lasted several hours.",
    Instructions: "How severe is the issue?",
    Levels: new List<string> { "Minor inconvenience", "Partial outage", "Major outage" }));

double score = answer.Value;
double confidence = answer.Confidence;
List<ScoreLevel> levels = answer.Levels;
```

Levels are ordered from low to high (2 to 10). `Value` is a probability-weighted position and can fall between level indexes; `Levels` carries each index, description, and probability.

## Generic call

`TypeSafeClient.EvaluateAsync` remains available for advanced requests, including several questions in one API call. It accepts a state and the raw named question payload, then returns a `JsonDocument`. Dispose that document after reading it. The typed `IJevService` methods above cover ordinary single-question calls.

## Package

Package ID: `Zilpio.TypeSafe.Jev`. Install it through NuGet using one of the commands above.

To build the package from source:

```sh
dotnet pack src/TypeSafe.Jev/TypeSafe.Jev.csproj -c Release
```
