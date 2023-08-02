namespace RinhaBackEnd.Extensions;

public static class JsonExtensions
{
    public static string ToJson(this object @object)
    {
        return JsonSerializer.Serialize(@object, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    public static HttpContent ToJsonHttpContent(this object @object)
    {
        return new StringContent(@object.ToJson(), Encoding.UTF8, "application/json");
    }

    public static T DeserializeTo<T>(this string json) {

        if (string.IsNullOrEmpty(json)) return default!;

        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        })!;
    }
}
