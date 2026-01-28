using System.Text.Json;

namespace Loremaster.Shared.Helpers;

public static class JsonHelper
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string Serialize<T>(T value, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Serialize(value, options ?? DefaultOptions);
    }

    public static T? Deserialize<T>(string json, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Deserialize<T>(json, options ?? DefaultOptions);
    }

    public static bool TryDeserialize<T>(string json, out T? result, JsonSerializerOptions? options = null)
    {
        try
        {
            result = JsonSerializer.Deserialize<T>(json, options ?? DefaultOptions);
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }
}
