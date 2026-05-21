using System.Net;
using System.Text;
using System.Text.Json;
using Api.Controllers;
using Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Api.Tests;

public class NewsControllerTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<NewsController>> _mockLogger;

    public NewsControllerTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<NewsController>>();

        _mockConfiguration
            .Setup(c => c["HackerNews:BaseAddress"])
            .Returns("https://hacker-news.firebaseio.com/v0/");
    }

    private static Mock<HttpMessageHandler> CreateHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) => respond(req));
        handler.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
        return handler;
    }

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

    private static HttpResponseMessage JsonOf<T>(T value) => Json(JsonSerializer.Serialize(value));

    private NewsController CreateController(HttpMessageHandler handler, IMemoryCache? cache = null)
    {
        var httpClient = new HttpClient(handler);
        return new NewsController(
            httpClient,
            _mockConfiguration.Object,
            cache ?? new MemoryCache(new MemoryCacheOptions()),
            _mockLogger.Object);
    }

    #region GetTopStories Tests

    [Fact]
    public async Task GetTopStories_WithValidResponse_ReturnStories()
    {
        var storyIds = new[] { 1, 2, 3, 4, 5 };
        var stories = new Dictionary<int, HackerNewsStory>
        {
            [1] = new() { Id = 1, Title = "Story 1", By = "user1", Score = 100, Time = 1234567890, Url = "http://example.com/1" },
            [2] = new() { Id = 2, Title = "Story 2", By = "user2", Score = 90, Time = 1234567891, Url = "http://example.com/2" },
            [3] = new() { Id = 3, Title = "Story 3", By = "user3", Score = 80, Time = 1234567892, Url = "http://example.com/3" }
        };

        var handler = CreateHandler(req =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path.EndsWith("topstories.json")) return JsonOf(storyIds);
            var id = ExtractId(path);
            return stories.TryGetValue(id, out var s) ? JsonOf(s) : Json("null");
        });
        var controller = CreateController(handler.Object);

        var result = await controller.GetTopStories(3);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStories = Assert.IsAssignableFrom<IEnumerable<HackerNewsStory>>(okResult.Value);
        Assert.Equal(3, returnedStories.Count());
    }

    [Fact]
    public async Task GetTopStories_WithEmptyResponse_ReturnsNotFound()
    {
        var handler = CreateHandler(_ => JsonOf(Array.Empty<int>()));
        var controller = CreateController(handler.Object);

        var result = await controller.GetTopStories(10);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task GetTopStories_WithNullResponse_ReturnsNotFound()
    {
        var handler = CreateHandler(_ => Json("null"));
        var controller = CreateController(handler.Object);

        var result = await controller.GetTopStories(10);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(51)]
    public async Task GetTopStories_WithInvalidCount_ClampsValue(int count)
    {
        var storyIds = Enumerable.Range(1, 100).ToArray();
        var handler = CreateHandler(req =>
        {
            if (req.RequestUri!.AbsolutePath.EndsWith("topstories.json")) return JsonOf(storyIds);
            var id = ExtractId(req.RequestUri.AbsolutePath);
            return JsonOf(new HackerNewsStory { Id = id, Title = "Test", By = "user", Score = 10, Time = 1234567890 });
        });
        var controller = CreateController(handler.Object);

        var result = await controller.GetTopStories(count);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStories = Assert.IsAssignableFrom<IEnumerable<HackerNewsStory>>(okResult.Value);
        Assert.True(returnedStories.Count() >= 1 && returnedStories.Count() <= 50);
    }

    [Fact]
    public async Task GetTopStories_UsesCache_OnSecondCall()
    {
        var storyIds = new[] { 1, 2, 3 };
        var handler = CreateHandler(req =>
        {
            if (req.RequestUri!.AbsolutePath.EndsWith("topstories.json")) return JsonOf(storyIds);
            return JsonOf(new HackerNewsStory { Id = 1, Title = "Test", By = "user", Score = 10, Time = 1234567890 });
        });
        var controller = CreateController(handler.Object);

        var result1 = await controller.GetTopStories(1);
        var result2 = await controller.GetTopStories(1);

        Assert.IsType<OkObjectResult>(result1.Result);
        Assert.IsType<OkObjectResult>(result2.Result);
        handler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsolutePath.EndsWith("topstories.json")),
            ItExpr.IsAny<CancellationToken>());
    }

    #endregion

    #region GetStory Tests

    [Fact]
    public async Task GetStory_WithValidId_ReturnStory()
    {
        var story = new HackerNewsStory { Id = 123, Title = "Test Story", By = "user", Score = 50, Time = 1234567890, Url = "http://example.com" };
        var handler = CreateHandler(_ => JsonOf(story));
        var controller = CreateController(handler.Object);

        var result = await controller.GetStory(123);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStory = Assert.IsType<HackerNewsStory>(okResult.Value);
        Assert.Equal(123, returnedStory.Id);
        Assert.Equal("Test Story", returnedStory.Title);
    }

    [Fact]
    public async Task GetStory_WithInvalidId_ReturnNotFound()
    {
        var handler = CreateHandler(_ => Json("null"));
        var controller = CreateController(handler.Object);

        var result = await controller.GetStory(999999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetStory_WithMissingUrl_ReturnStoryWithoutUrl()
    {
        var story = new HackerNewsStory { Id = 456, Title = "Text Post", By = "user2", Score = 25, Time = 1234567890, Url = null };
        var handler = CreateHandler(_ => JsonOf(story));
        var controller = CreateController(handler.Object);

        var result = await controller.GetStory(456);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStory = Assert.IsType<HackerNewsStory>(okResult.Value);
        Assert.Null(returnedStory.Url);
        Assert.Equal("Text Post", returnedStory.Title);
    }

    [Fact]
    public async Task GetStory_CachesBetweenCalls()
    {
        var story = new HackerNewsStory { Id = 789, Title = "Cached Story", By = "user3", Score = 75, Time = 1234567890 };
        var handler = CreateHandler(_ => JsonOf(story));
        var controller = CreateController(handler.Object);

        var result1 = await controller.GetStory(789);
        var result2 = await controller.GetStory(789);

        Assert.IsType<OkObjectResult>(result1.Result);
        Assert.IsType<OkObjectResult>(result2.Result);
        handler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GetStory_WithHttpException_ReturnsNotFound()
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("404 Not Found"));
        handler.Protected().Setup("Dispose", ItExpr.IsAny<bool>());
        var controller = CreateController(handler.Object);

        var result = await controller.GetStory(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetStory_WithNullAuthor_ReturnStory()
    {
        var story = new HackerNewsStory { Id = 100, Title = "No Author", By = null, Score = 15, Time = 1234567890, Url = "http://test.com" };
        var handler = CreateHandler(_ => JsonOf(story));
        var controller = CreateController(handler.Object);

        var result = await controller.GetStory(100);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStory = Assert.IsType<HackerNewsStory>(okResult.Value);
        Assert.Null(returnedStory.By);
    }

    [Fact]
    public async Task GetStory_WithZeroScore_ReturnStory()
    {
        var story = new HackerNewsStory { Id = 200, Title = "Zero Score", By = "user", Score = 0, Time = 1234567890 };
        var handler = CreateHandler(_ => JsonOf(story));
        var controller = CreateController(handler.Object);

        var result = await controller.GetStory(200);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStory = Assert.IsType<HackerNewsStory>(okResult.Value);
        Assert.Equal(0, returnedStory.Score);
    }

    [Fact]
    public async Task GetTopStories_WithSomeFailedRequests_ReturnPartialResults()
    {
        var storyIds = new[] { 1, 2, 3, 4, 5 };
        var handler = CreateHandler(req =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path.EndsWith("topstories.json")) return JsonOf(storyIds);
            var id = ExtractId(path);
            if (id == 2) return Json("null");
            return JsonOf(new HackerNewsStory { Id = id, Title = $"Story {id}", By = "user", Score = 50, Time = 1234567890 });
        });
        var controller = CreateController(handler.Object);

        var result = await controller.GetTopStories(3);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStories = Assert.IsAssignableFrom<IEnumerable<HackerNewsStory>>(okResult.Value);
        Assert.Equal(2, returnedStories.Count());
    }

    #endregion

    private static int ExtractId(string path)
    {
        var fileName = Path.GetFileNameWithoutExtension(path);
        return int.TryParse(fileName, out var id) ? id : 0;
    }
}
