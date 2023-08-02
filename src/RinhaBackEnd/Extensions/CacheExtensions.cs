using Microsoft.Extensions.Caching.Distributed;

namespace RinhaBackEnd.Extensions;

public static class CacheExtensions
{
    public static T GetOrCreate<T>(this IDistributedCache cache, string key, Func<T> action, DistributedCacheEntryOptions options) where T : class
    {
        var retriveCache = cache.Get(key);

        if (retriveCache != null) return Encoding.UTF8.GetString(retriveCache).DeserializeTo<T>();

        dynamic execute = action.Invoke();
        T result;

        result = execute is Task ? execute.ConfigureAwait(false).GetAwaiter().GetResult() : execute;

        if (result != null)
        {
            var json = result.ToJson();
            cache.Set(key, Encoding.UTF8.GetBytes(json), options);
            return result;
        }
        return null!;
    }

    public static string GetOrCreateString<T>(this IDistributedCache cache, string key, Func<T> action, DistributedCacheEntryOptions options) where T : class
    {
        var retriveCache = cache.GetString(key);
        
        if (!string.IsNullOrEmpty(retriveCache)) return retriveCache;

        dynamic execute = action.Invoke();
        object result = execute is Task ? execute.ConfigureAwait(false).GetAwaiter().GetResult() : execute;

        if (result != null)
        {
            var json = result.ToJson();
            cache.Set(key, Encoding.UTF8.GetBytes(json), options);
            return json;
        }

        return null!;
    }
}
