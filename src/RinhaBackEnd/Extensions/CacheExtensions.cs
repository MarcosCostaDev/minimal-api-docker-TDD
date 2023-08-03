namespace RinhaBackEnd.Extensions;

public static class CacheExtensions
{
    public static async Task<T> GetOrCreateAsync<T>(this IDistributedCache cache, string key, Func<T> action, DistributedCacheEntryOptions options = null!) where T : class
    {
        var retriveCache = cache.Get(key);

        if (retriveCache != null) return Encoding.UTF8.GetString(retriveCache).DeserializeTo<T>();

        options ??= new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromSeconds(30) };

        dynamic execute = action.Invoke();

        T result = execute is Task ? await execute : execute;

        if (result == null) return null!;

        var json = result.ToJson();

        cache.Set(key, Encoding.UTF8.GetBytes(json), options);

        return result;
    }

    public static async Task<string> GetOrCreateStringAsync<T>(this IDistributedCache cache, string key, Func<T> action, DistributedCacheEntryOptions options = null!) where T : class
    {
        var retriveCache = cache.GetString(key);

        if (!string.IsNullOrEmpty(retriveCache)) return retriveCache;

        options ??= new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromSeconds(30) };

        dynamic execute = action.Invoke();

        object result = execute is Task ? await execute : execute;

        if (result == null) return null!;

        var json = result.ToJson();

        cache.Set(key, Encoding.UTF8.GetBytes(json), options);

        return json;
    }
}
