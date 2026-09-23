using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TypeSafe.Jev;
using Xunit;

namespace TypeSafe.Jev.Tests;

public sealed class TypeSafeClientTests
{
    [Fact]
    public async Task RegistersInjectableTransientServiceWithHttpClientFactory()
    {
        var services = new ServiceCollection();
        var handler = new RecordingHandler("{\"answers\":{\"result\":{\"type\":\"noul\",\"noul\":0.93}}}");
        services.AddJevService("test-key").ConfigurePrimaryHttpMessageHandler(() => handler);

        var registration = Assert.Single(services, x => x.ServiceType == typeof(IJevService));
        Assert.Equal(ServiceLifetime.Transient, registration.Lifetime);
        using var provider = services.BuildServiceProvider();
        Assert.NotSame(provider.GetRequiredService<IJevService>(), provider.GetRequiredService<IJevService>());
        Assert.NotNull(provider.GetRequiredService<IHttpClientFactory>());
        var answer = await provider.GetRequiredService<IJevService>()
            .Noul(new NoulRequest("Please call me today", "Is this urgent?"));
        Assert.Equal(0.93, answer.Probability);
        Assert.Equal("Bearer test-key", handler.Authorization);
    }

    [Fact]
    public async Task NoulSendsTypedQuestionAndReturnsProbability()
    {
        var handler = new RecordingHandler("{\"answers\":{\"result\":{\"type\":\"noul\",\"noul\":0.93}}}");
        IJevService jev = new JevService("test-key", new HttpClient(handler));

        var answer = await jev.Noul(new NoulRequest("I need help today", "Is this urgent?",
            new NoulCriteria("Explicit time pressure", "No time pressure")));

        Assert.Equal(0.93, answer.Probability);
        Assert.Equal("https://api.typesafe.ai/v1/systemone", handler.Uri);
        Assert.Equal("Bearer test-key", handler.Authorization);
        using var body = JsonDocument.Parse(handler.Body!);
        Assert.Equal("jev-latest", body.RootElement.GetProperty("model").GetString());
        Assert.Equal("I need help today", body.RootElement.GetProperty("state").GetString());
        var question = body.RootElement.GetProperty("questions").GetProperty("result");
        Assert.Equal("noul", question.GetProperty("type").GetString());
        Assert.Equal("Explicit time pressure", question.GetProperty("criteria").GetProperty("true").GetString());
    }

    [Fact]
    public async Task ChoiceSendsOptionsAndReturnsProbabilitiesAsList()
    {
        var handler = new RecordingHandler("{\"answers\":{\"result\":{\"type\":\"choice\",\"choice\":\"support\",\"confidence\":0.8,\"probabilities\":{\"support\":0.9,\"sales\":0.1}}}}");
        IJevService jev = new JevService("test-key", new HttpClient(handler));

        var answer = await jev.Choice(new ChoiceRequest("My app crashes", "Which team should help?",
            new List<ChoiceOption> { new("support", "Technical issues"), new("sales", "Purchasing") }));

        Assert.Equal("support", answer.Value);
        Assert.Equal(0.8, answer.Confidence);
        Assert.Equal(2, answer.Probabilities.Count);
        Assert.Contains(answer.Probabilities, x => x.Key == "support" && x.Probability == 0.9);
        using var body = JsonDocument.Parse(handler.Body!);
        var criteria = body.RootElement.GetProperty("questions").GetProperty("result").GetProperty("criteria");
        Assert.Equal("Technical issues", criteria.GetProperty("support").GetString());
    }

    [Fact]
    public async Task ScoreSendsLevelsAndReturnsLegendAsList()
    {
        var handler = new RecordingHandler("{\"answers\":{\"result\":{\"type\":\"score\",\"score\":1.2,\"confidence\":0.7,\"legend\":{\"0\":\"Low\",\"1\":\"Medium\",\"2\":\"High\"},\"probabilities\":{\"0\":0.0,\"1\":0.8,\"2\":0.2}}}}");
        IJevService jev = new JevService("test-key", new HttpClient(handler));

        var answer = await jev.Score(new ScoreRequest("Several delays", "How severe is the issue?",
            new List<string> { "Low", "Medium", "High" }));

        Assert.Equal(1.2, answer.Value);
        Assert.Equal(0.7, answer.Confidence);
        Assert.Equal(new List<ScoreLevel> { new(0, "Low", 0), new(1, "Medium", 0.8), new(2, "High", 0.2) }, answer.Levels);
        using var body = JsonDocument.Parse(handler.Body!);
        Assert.Equal(3, body.RootElement.GetProperty("questions").GetProperty("result").GetProperty("criteria").GetArrayLength());
    }

    [Fact]
    public async Task GenericCallStillSupportsSeveralQuestions()
    {
        var handler = new RecordingHandler("{\"answers\":{\"urgent\":{\"type\":\"noul\",\"noul\":0.93},\"team\":{\"type\":\"choice\",\"choice\":\"support\",\"confidence\":1,\"probabilities\":{\"support\":1}}}}");
        var client = new TypeSafeClient(new HttpClient(handler), "test-key");
        using var response = await client.EvaluateAsync("I need help today", new Dictionary<string, object>
        {
            ["urgent"] = new { type = "noul", instructions = "Is this urgent?" },
            ["team"] = new { type = "choice", instructions = "Which team?", criteria = new { support = "Technical support", sales = "Purchasing" } }
        });

        Assert.Equal(0.93, response.RootElement.GetProperty("answers").GetProperty("urgent").GetProperty("noul").GetDouble());
        Assert.Equal("support", response.RootElement.GetProperty("answers").GetProperty("team").GetProperty("choice").GetString());
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly string _response;
        public RecordingHandler(string response) => _response = response;
        public string? Uri { get; private set; }
        public string? Authorization { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Uri = request.RequestUri?.ToString();
            Authorization = request.Headers.Authorization?.ToString();
            Body = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(_response) };
        }
    }
}
