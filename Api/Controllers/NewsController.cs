using System.Net.Http.Json;
using Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NewsController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<NewsController> _logger;
        private const string TopStoriesCacheKey = "top_stories_ids";
        private const string StoryCacheKeyPrefix = "story_";
        private static readonly TimeSpan StoryIdsCacheDuration = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan StoryCacheDuration = TimeSpan.FromHours(1);

        public NewsController(HttpClient httpClient, IConfiguration configuration, IMemoryCache cache, ILogger<NewsController> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            var baseAddress = configuration["HackerNews:BaseAddress"];
            _httpClient.BaseAddress = new Uri(baseAddress ?? "https://hacker-news.firebaseio.com/v0/");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Api/1.0");
            _logger = logger;
        }

        [HttpGet("top")]
        public async Task<ActionResult<IEnumerable<HackerNewsStory>>> GetTopStories(int count = 10, CancellationToken cancellationToken = default)
        {
            int[] storyIds;

            if (!_cache.TryGetValue(TopStoriesCacheKey, out int[]? cachedIds))
            {
                storyIds = await _httpClient.GetFromJsonAsync<int[]>("topstories.json", cancellationToken) ?? Array.Empty<int>();
                if (storyIds.Length > 0)
                {
                    _cache.Set(TopStoriesCacheKey, storyIds, StoryIdsCacheDuration);
                    _logger.LogInformation("Cached top story IDs ({Count} stories)", storyIds.Length);
                }
            }
            else
            {
                storyIds = cachedIds ?? Array.Empty<int>();
                _logger.LogInformation("Using cached top story IDs");
            }

            if (storyIds.Length == 0)
            {
                return NotFound("No top stories were returned by the Hacker News API.");
            }

            count = Math.Clamp(count, 1, 50);
            var topIds = storyIds.Take(count);
            var stories = new List<HackerNewsStory>();

            foreach (var id in topIds)
            {
                var story = await FetchStoryAsync(id, cancellationToken);
                if (story is not null)
                {
                    stories.Add(story);
                }
            }

            return Ok(stories);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<HackerNewsStory>> GetStory(int id, CancellationToken cancellationToken = default)
        {
            var story = await FetchStoryAsync(id, cancellationToken);
            return story is null ? NotFound() : Ok(story);
        }

        private async Task<HackerNewsStory?> FetchStoryAsync(int id, CancellationToken cancellationToken)
        {
            var cacheKey = $"{StoryCacheKeyPrefix}{id}";

            if (_cache.TryGetValue(cacheKey, out HackerNewsStory? cachedStory))
            {
                _logger.LogInformation("Cache hit for story {Id}", id);
                return cachedStory;
            }

            try
            {
                var story = await _httpClient.GetFromJsonAsync<HackerNewsStory>($"item/{id}.json", cancellationToken);
                if (story is not null)
                {
                    _cache.Set(cacheKey, story, StoryCacheDuration);
                    _logger.LogInformation("Cached story {Id}", id);
                }
                return story;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch Hacker News item {Id}", id);
                return null;
            }
        }
    }
}
