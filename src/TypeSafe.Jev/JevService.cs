using System.Text.Json;

namespace TypeSafe.Jev;

public interface IJevService
{
    Task<NoulAnswer> Noul(NoulRequest request, CancellationToken cancellationToken = default);
    Task<ChoiceAnswer> Choice(ChoiceRequest request, CancellationToken cancellationToken = default);
    Task<ScoreAnswer> Score(ScoreRequest request, CancellationToken cancellationToken = default);
}

public sealed class JevService : IJevService
{
    private readonly TypeSafeClient _client;

    public JevService(string apiKey, HttpClient httpClient) => _client = new TypeSafeClient(httpClient, apiKey);

    public async Task<NoulAnswer> Noul(NoulRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request.State, request.Instructions);
        object question = request.Criteria is null
            ? new { type = "noul", instructions = request.Instructions }
            : new { type = "noul", instructions = request.Instructions,
                criteria = new { @true = request.Criteria.Yes, @false = request.Criteria.No } };
        using var response = await Ask(request.State, question, cancellationToken);
        return new NoulAnswer(Answer(response).GetProperty("noul").GetDouble());
    }

    public async Task<ChoiceAnswer> Choice(ChoiceRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request.State, request.Instructions);
        if (request.Options is null || request.Options.Count is < 2 or > 255
            || request.Options.Any(x => string.IsNullOrWhiteSpace(x.Key) || string.IsNullOrWhiteSpace(x.Description))
            || request.Options.Select(x => x.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count() != request.Options.Count)
            throw new ArgumentException("Choice requires 2 to 255 distinct options with descriptions.", nameof(request));

        var criteria = request.Options.ToDictionary(x => x.Key, x => x.Description);
        using var response = await Ask(request.State,
            new { type = "choice", instructions = request.Instructions, criteria }, cancellationToken);
        var answer = Answer(response);
        return new ChoiceAnswer(answer.GetProperty("choice").GetString()!, answer.GetProperty("confidence").GetDouble(),
            answer.GetProperty("probabilities").EnumerateObject()
                .Select(x => new JevProbability(x.Name, x.Value.GetDouble())).ToList());
    }

    public async Task<ScoreAnswer> Score(ScoreRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request.State, request.Instructions);
        if (request.Levels is null || request.Levels.Count is < 2 or > 10 || request.Levels.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Score requires 2 to 10 described levels.", nameof(request));

        using var response = await Ask(request.State,
            new { type = "score", instructions = request.Instructions, criteria = request.Levels }, cancellationToken);
        var answer = Answer(response);
        var probabilities = answer.GetProperty("probabilities");
        var levels = answer.GetProperty("legend").EnumerateObject()
            .Select(x => new ScoreLevel(int.Parse(x.Name), x.Value.GetString()!, probabilities.GetProperty(x.Name).GetDouble()))
            .OrderBy(x => x.Index).ToList();
        return new ScoreAnswer(answer.GetProperty("score").GetDouble(), answer.GetProperty("confidence").GetDouble(), levels);
    }

    private Task<JsonDocument> Ask(string state, object question, CancellationToken cancellationToken) =>
        _client.EvaluateAsync(state, new Dictionary<string, object> { ["result"] = question }, cancellationToken: cancellationToken);

    private static JsonElement Answer(JsonDocument response) =>
        response.RootElement.GetProperty("answers").GetProperty("result");

    private static void Validate(string state, string instructions)
    {
        if (string.IsNullOrWhiteSpace(state)) throw new ArgumentException("State is required.", nameof(state));
        if (string.IsNullOrWhiteSpace(instructions)) throw new ArgumentException("Instructions are required.", nameof(instructions));
    }
}
