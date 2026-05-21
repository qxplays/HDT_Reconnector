using System.Text.Json;

namespace BgMetaApi.Services;

public static class MetaJsonValidator
{
    public static (bool Success, string? Error) Validate(string kind, string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return (false, "Empty body");

        var trimmed = json.TrimStart();
        if (!trimmed.StartsWith('['))
            return (false, "Expected JSON array");

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return (false, "Root must be an array");

            var count = doc.RootElement.GetArrayLength();
            if (count == 0)
                return (false, "Array is empty");

            var idField = kind == "heroes" ? "hero_dbf_id" : "trinket_dbf_id";
            var withId = 0;
            foreach (var row in doc.RootElement.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Object)
                    continue;

                if (row.TryGetProperty(idField, out var id) && id.ValueKind == JsonValueKind.Number)
                    withId++;
            }

            if (withId == 0)
                return (false, $"No rows with {idField}");

            return (true, null);
        }
        catch (JsonException ex)
        {
            return (false, "Invalid JSON: " + ex.Message);
        }
    }
}
