using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TypeSafe.Jev;

/// <summary>Raw client for POST /v1/systemone. The caller owns the HttpClient.</summary>
public sealed class TypeSafeClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public TypeSafeClient(HttpClient http, string apiKey)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _apiKey = !string.IsNullOrWhiteSpace(apiKey) ? apiKey : throw new ArgumentException("TypeSafe API key is required.", nameof(apiKey));
    }

    public async Task<JsonDocument> EvaluateAsync(object state, IReadOnlyDictionary<string, object> questions,
        string model = "jev-latest", CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (questions is null || questions.Count == 0) throw new ArgumentException("At least one question is required.", nameof(questions));
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.typesafe.ai/v1/systemone")
        {
            Content = JsonContent.Create(new { state, model, questions })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
