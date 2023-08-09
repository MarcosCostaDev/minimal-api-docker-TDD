namespace RinhaBackEnd.Extensions;

public static class CacheExtensions
{
    public static async Task<string> GetOrCreateStringAsync<T>(this IConnectionMultiplexer redis, string key, Func<T> action, TimeSpan? timeSpan = null) where T : class
    {
        var db = redis.GetDatabase();
        var retriveCache = db.StringGet(key);

        if (!string.IsNullOrEmpty(retriveCache)) return retriveCache;


        dynamic execute = action.Invoke();

        object result = execute is Task ? await execute : execute;

        if (result == null) return null!;

        var json = result.ToJson();

        db.StringSet(key, Encoding.UTF8.GetBytes(json), expiry: timeSpan ?? TimeSpan.FromMinutes(3));

        return json;
    }
}
