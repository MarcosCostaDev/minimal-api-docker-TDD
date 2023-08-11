namespace RinhaBackEnd.Extensions;

public static class CacheExtensions
{

    public static async Task<string> GetOrCreateStringAsync<T>(this IDistributedCache cache, string key, Func<T> action, DistributedCacheEntryOptions options = null!, CancellationToken cancellationToken = default) where T : class
    {
        var retriveCache = await cache.GetStringAsync(key, cancellationToken);

        if (!string.IsNullOrEmpty(retriveCache)) return retriveCache;

        options ??= new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromSeconds(30) };

        dynamic execute = action.Invoke();

        object result = execute is Task ? await execute : execute;

        if (result == null) return null!;

        var json = result.ToJson();

        await cache.SetAsync(key, Encoding.UTF8.GetBytes(json), options, cancellationToken);

        return json;
    }
}
