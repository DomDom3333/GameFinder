using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.RateLimiting;
using GameFinderApi.Objects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace GameFinderApi.Controllers
{
    [ApiController]
    [Route("[controller]/{id}")]
    public class SteamMarketDataController : ControllerBase
    {
        private readonly ILogger<SteamMarketDataController> _logger;
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _memoryCache;
        private static readonly SemaphoreSlim ApiSemaphore = new(1, 1);
        private static readonly TokenBucketRateLimiter RateLimiter = new(
            new TokenBucketRateLimiterOptions
            {
                TokenLimit = 60,
                TokensPerPeriod = 60,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 100,
                AutoReplenishment = true
            });
        private static readonly ConcurrentDictionary<string, Task<GameData?>> OngoingRequests = new();

        public SteamMarketDataController(ILogger<SteamMarketDataController> logger, HttpClient httpClient, IMemoryCache memoryCache)
        {
            _logger = logger;
            _httpClient = httpClient;
            _memoryCache = memoryCache;
        }

        [HttpGet]
        public async Task<ActionResult<GameData>> Get(string id)
        {
            if (_memoryCache.TryGetValue(id, out GameData? cached))
            {
                return Ok(cached);
            }

            if (GameDataCache.TryGet(id, out GameData? persistent))
            {
                _memoryCache.Set(id, persistent, TimeSpan.FromDays(7));
                return Ok(persistent);
            }

            var fetchTask = OngoingRequests.GetOrAdd(id, _ => FetchGameDataAsync(id));
            var data = await fetchTask;
            OngoingRequests.TryRemove(id, out _);

            if (data == null)
            {
                return NoContent();
            }

            return Ok(data);
        }

        private async Task<GameData?> FetchGameDataAsync(string id)
        {
            RateLimitLease lease = await RateLimiter.AcquireAsync(1);
            if (!lease.IsAcquired)
            {
                return null;
            }

            await ApiSemaphore.WaitAsync();
            try
            {
                string apiUrl = $"https://store.steampowered.com/api/appdetails?appids={id}";
                string jsonData = await _httpClient.GetStringAsync(apiUrl);
                JsonDocument jsonDoc = JsonDocument.Parse(jsonData);
                bool isSuccess = jsonDoc.RootElement.GetProperty(id).GetProperty("success").GetBoolean();
                if (!isSuccess)
                {
                    return null;
                }

                GameData? gamedata = jsonDoc.RootElement.GetProperty(id).GetProperty("data")
                    .Deserialize<GameData>();

                if (gamedata != null && gamedata.AppType == "game" && gamedata.Categories.Any(x => x.Id == 1))
                {
                    gamedata.SupportedLanguages = Misc.RemoveHtmlTags(gamedata.SupportedLanguages);

                    // fetch review summary
                    string reviewUrl = $"https://store.steampowered.com/appreviews/{id}?json=1";
                    string reviewJson = await _httpClient.GetStringAsync(reviewUrl);
                    JsonDocument reviewDoc = JsonDocument.Parse(reviewJson);
                    gamedata.ReviewSummary = reviewDoc.RootElement.GetProperty("query_summary").Deserialize<ReviewSummary>();

                    _memoryCache.Set(id, gamedata, TimeSpan.FromDays(7));
                    await GameDataCache.SetAsync(id, gamedata);
                    return gamedata;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
            finally
            {
                ApiSemaphore.Release();
            }
        }
    }
}