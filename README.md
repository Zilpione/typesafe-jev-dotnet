# TypeSafe.Jev .NET Wrapper

An independent .NET wrapper for [Jev / TypeSafe System One](https://docs.typesafe.ai/api). It is not an official TypeSafe AI SDK.

Jev evaluates text against typed questions. This wrapper exposes `IJevService` with three methods: `Noul` for a yes/no probability, `Choice` for one option from a list, and `Score` for a position across ordered levels. Each method returns a typed result.

## Setup

Reference `src/TypeSafe.Jev/TypeSafe.Jev.csproj` from your .NET project. Get a TypeSafe API key from the [TypeSafe dashboard](https://console.typesafe.ai/) and keep it in an environment variable or your application's secret store. The key is never part of the request models.

Create one `HttpClient` and one service instance; reuse both across calls:

```csharp
using TypeSafe.Jev;

var apiKey = Environment.GetEnvironmentVariable("TYPESAFE_API_KEY")
    ?? throw new InvalidOperationException("TYPESAFE_API_KEY is missing.");
using var http = new HttpClient();
IJevService jev = new JevService(apiKey, http);
```

The caller owns `HttpClient`. In ASP.NET Core, register `JevService` as a typed HTTP client and read the key from configuration. Each method also accepts an optional `CancellationToken`.

## Noul: yes/no probability

```csharp
var answer = await jev.Noul(new NoulRequest(
    State: "Please call me back today; this is urgent.",
    Instructions: "Does the message express urgency?"));

double probabilityOfYes = answer.Probability; // 0 to 1
bool isUrgent = probabilityOfYes >= 0.8;        // choose your own threshold
```

`Probability` is the likelihood of **yes**, not a Boolean. Add `new NoulCriteria("What yes means", "What no means")` as the third argument when the boundary needs clarification.

## Choice: pick one option

```csharp
var answer = await jev.Choice(new ChoiceRequest(
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
var answer = await jev.Score(new ScoreRequest(
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

## Build

```sh
dotnet test tests/TypeSafe.Jev.Tests/TypeSafe.Jev.Tests.csproj
dotnet pack src/TypeSafe.Jev/TypeSafe.Jev.csproj -c Release
```

The package targets .NET 6. Source on GitHub and a locally built package do not publish it to NuGet.org.
