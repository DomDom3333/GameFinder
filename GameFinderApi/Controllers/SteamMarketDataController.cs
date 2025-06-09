using System;
using System.Text.Json;
using System.Threading.Tasks;
using GameFinder.Objects;
using GameFinder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace GameFinder.Controllers
{
    [ApiController]
    [Route("[controller]/{id}")]
    public class SteamMarketDataController : ControllerBase
    {
        private readonly ILogger<SteamMarketDataController> _logger;
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _memoryCache;
        private static readonly SemaphoreSlim ApiSemaphore = new(1, 1);

        public SteamMarketDataController(ILogger<SteamMarketDataController> logger, HttpClient httpClient, IMemoryCache memoryCache)
        {
            _logger = logger;
            _httpClient = httpClient;
            _memoryCache = memoryCache;
        }

        [HttpGet]
        public async Task<ActionResult<GameData>> Get(string id)
        {
            // Check in-memory cache
            if (_memoryCache.TryGetValue(id, out GameData? cachedGameData))
            {
                return Ok(cachedGameData);
            }

            // Check persistent cache
            if (GameDataCache.TryGet(id, out GameData? cachedPersistent))
            {
                _memoryCache.Set(id, cachedPersistent, TimeSpan.FromDays(7));
                return Ok(cachedPersistent);
            }

            string apiUrl = $"https://store.steampowered.com/api/appdetails?appids={id}";
            try
            {
                await ApiSemaphore.WaitAsync();
                string jsonData = await _httpClient.GetStringAsync(apiUrl);
                JsonDocument jsonDoc = JsonDocument.Parse(jsonData);
                bool isSuccess = jsonDoc.RootElement.GetProperty(id).GetProperty("success").GetBoolean();
                if (!isSuccess)
                {
                    return NoContent();
                }

                GameData? gamedata = jsonDoc.RootElement.GetProperty(id).GetProperty("data")
                    .Deserialize<GameData>();

                if (gamedata != null && gamedata.AppType == "game" && gamedata.Categories.Any(x => x.Id == 1))
                {
                    gamedata.SupportedLanguages = Misc.RemoveHtmlTags(gamedata.SupportedLanguages);
                    // Cache the data for one week and persist to disk
                    _memoryCache.Set(id, gamedata, TimeSpan.FromDays(7));
                    await GameDataCache.SetAsync(id, gamedata);
                    return Ok(gamedata);
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Problem();
            }
            finally
            {
                ApiSemaphore.Release();
            }
        }
    }
}