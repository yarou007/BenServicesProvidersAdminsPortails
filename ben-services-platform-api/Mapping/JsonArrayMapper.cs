using System.Text.Json;

namespace BenServicesPlatform.Api.Mapping;

public static class JsonArrayMapper
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static string Serialize(string[] values)
    {
        return JsonSerializer.Serialize(values ?? [], SerializerOptions);
    }

    public static string[] Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(json, SerializerOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
