using System.Net;
using System.Text.Json;
using TypeSafe.Jev;
using Xunit;

namespace TypeSafe.Jev.Tests;

public sealed class TypeSafeClientTests
{
    [Fact]
    public async Task SendsRawQuestionsWithBearerAndReturnsTypedAnswers()
    {
        var handler = new RecordingHandler();
        var client = new TypeSafeClient(new HttpClient(handler), "test-key");
        using var response = await client.EvaluateAsync("Bolletta gas",
            new Dictionary<string, object>
            {
                ["bill"] = new { type = "noul", instructions = "Is this a bill?" },
                ["kind"] = new { type = "choice", instructions = "Which utility?", criteria = new { gas = "Gas", power = "Electricity" } },
                ["quality"] = new { type = "score", instructions = "How clear?", criteria = new[] { "Poor", "Clear" } }
            });

        Assert.Equal("https://api.typesafe.ai/v1/systemone", handler.Uri);
        Assert.Equal("Bearer test-key", handler.Authorization);
        using var request = JsonDocument.Parse(handler.Body!);
        Assert.Equal("jev-latest", request.RootElement.GetProperty("model").GetString());
        Assert.Equal("Bolletta gas", request.RootElement.GetProperty("state").GetString());
        Assert.Equal("noul", request.RootElement.GetProperty("questions").GetProperty("bill").GetProperty("type").GetString());
        Assert.Equal("gas", response.RootElement.GetProperty("answers").GetProperty("kind").GetProperty("choice").GetString());
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string? Uri { get; private set; }
        public string? Authorization { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Uri = request.RequestUri?.ToString();
            Authorization = request.Headers.Authorization?.ToString();
            Body = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"model\":\"jev-latest\",\"answers\":{\"kind\":{\"type\":\"choice\",\"choice\":\"gas\"}},\"usage\":{\"input_tokens\":1,\"output_tokens\":1}}")
            };
        }
    }
}
