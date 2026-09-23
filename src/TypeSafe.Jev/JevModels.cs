namespace TypeSafe.Jev;

public sealed record NoulCriteria(string Yes, string No);
public sealed record NoulRequest(string State, string Instructions, NoulCriteria? Criteria = null);
public sealed record NoulAnswer(double Probability);

public sealed record ChoiceOption(string Key, string Description);
public sealed record ChoiceRequest(string State, string Instructions, List<ChoiceOption> Options);
public sealed record JevProbability(string Key, double Probability);
public sealed record ChoiceAnswer(string Value, double Confidence, List<JevProbability> Probabilities);

public sealed record ScoreRequest(string State, string Instructions, List<string> Levels);
public sealed record ScoreLevel(int Index, string Description, double Probability);
public sealed record ScoreAnswer(double Value, double Confidence, List<ScoreLevel> Levels);
