using System.Text.Json;
using GameFinder.Objects;

namespace GameFinder;

public static class GameDataCache
{
    private static readonly string CacheFilePath = Path.Combine(AppContext.BaseDirectory, "gameCache.json");
    private static readonly SemaphoreSlim FileLock = new(1, 1);
    private static readonly Dictionary<string, GameData> _cache = LoadCache();

    private static Dictionary<string, GameData> LoadCache()
    {
        try
        {
            if (File.Exists(CacheFilePath))
            {
                string json = File.ReadAllText(CacheFilePath);
                var data = JsonSerializer.Deserialize<Dictionary<string, GameData>>(json);
                if (data != null)
                    return data;
            }
        }
        catch
        {
            // ignore load errors
        }

        return new Dictionary<string, GameData>();
    }

    public static bool TryGet(string id, out GameData? data)
    {
        lock (_cache)
        {
            return _cache.TryGetValue(id, out data);
        }
    }

    public static async Task SetAsync(string id, GameData data)
    {
        lock (_cache)
        {
            _cache[id] = data;
        }

        await FileLock.WaitAsync();
        try
        {
            string json = JsonSerializer.Serialize(_cache);
            await File.WriteAllTextAsync(CacheFilePath, json);
        }
        catch
        {
            // ignore save errors
        }
        finally
        {
            FileLock.Release();
        }
    }
}
