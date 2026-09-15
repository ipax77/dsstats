using System.Security.Cryptography;
using System.Text.Json;

namespace dsstats.web.AI;

public sealed record PromptExample(string Question, WinrateQuery Answer);
public sealed record PromptMessage(string Role, string Content);

public sealed class WinratePrompt
{
    public required int FormatVersion { get; init; }
    public required string PromptVersion { get; init; }
    public required string[] SystemMessages { get; init; }
    public required PromptExample[] Examples { get; init; }
    public required JsonElement ResponseSchema { get; init; }
    [System.Text.Json.Serialization.JsonIgnore] public string Hash { get; private set; } = "";

    public static WinratePrompt Load(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var prompt = JsonSerializer.Deserialize<WinratePrompt>(bytes, WinrateQuery.JsonOptions)
            ?? throw new InvalidDataException("Empty prompt bundle.");
        if (prompt.FormatVersion != 1 || string.IsNullOrWhiteSpace(prompt.PromptVersion) ||
            prompt.SystemMessages.Length == 0 || prompt.SystemMessages.Any(string.IsNullOrWhiteSpace) ||
            prompt.ResponseSchema.ValueKind != JsonValueKind.Object ||
            !prompt.ResponseSchema.TryGetProperty("type", out var type) || type.GetString() != "object")
            throw new InvalidDataException("Invalid or unsupported prompt bundle.");
        if (!prompt.ResponseSchema.TryGetProperty("additionalProperties", out var additional) || additional.ValueKind != JsonValueKind.False ||
            !prompt.ResponseSchema.TryGetProperty("properties", out var properties) || properties.ValueKind != JsonValueKind.Object ||
            !prompt.ResponseSchema.TryGetProperty("required", out var required) || required.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("The response schema must declare a closed object with required properties.");
        var fields = typeof(WinrateQuery).GetProperties().Select(p => JsonNamingPolicy.CamelCase.ConvertName(p.Name)).ToHashSet();
        if (!fields.SetEquals(properties.EnumerateObject().Select(p => p.Name)) ||
            !fields.SetEquals(required.EnumerateArray().Select(p => p.GetString() ?? "")))
            throw new InvalidDataException("The response schema fields must match the winrate query contract.");
        foreach (var example in prompt.Examples)
        {
            if (string.IsNullOrWhiteSpace(example.Question)) throw new InvalidDataException("Empty example question.");
            example.Answer.Validate();
        }
        prompt.Hash = Convert.ToHexString(SHA256.HashData(bytes));
        return prompt;
    }

    public IReadOnlyList<PromptMessage> Messages(string question)
    {
        if (string.IsNullOrWhiteSpace(question) || question.Length > 2000)
            throw new ArgumentException("Enter a question of 1–2000 characters.");
        // One initial system message works with both browser sessions and local model chat templates.
        List<PromptMessage> messages = [new("system", string.Join("\n\n", SystemMessages))];
        foreach (var example in Examples)
        {
            messages.Add(new("user", example.Question));
            messages.Add(new("assistant", JsonSerializer.Serialize(example.Answer, WinrateQuery.JsonOptions)));
        }
        messages.Add(new("user", question));
        return messages;
    }
}
