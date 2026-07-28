using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tansekak.IntegrationTests;

public class PublicApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PublicApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Config_ReturnsCurrentYear()
    {
        var response = await _client.GetAsync("/api/config");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<ApiEnvelope<ConfigData>>();
        Assert.NotNull(json);
        Assert.True(json!.Success);
        Assert.Equal(2025, json.Data!.CurrentYear);
    }

    [Fact]
    public async Task Predict_ReturnsPagedResultsWithTotalCount()
    {
        var page1 = await _client.PostAsJsonAsync("/api/admission/predict", new
        {
            track = "Science",
            score = 300,
            page = 1,
            pageSize = 5
        });
        Assert.Equal(HttpStatusCode.OK, page1.StatusCode);
        var first = await page1.Content.ReadFromJsonAsync<ApiEnvelope<PredictData>>();
        Assert.NotNull(first?.Data);
        Assert.True(first!.Data!.TotalCount > 5);
        Assert.Equal(5, first.Data!.Results.Length);
        Assert.True(first.Data!.HasMore);

        var page2 = await _client.PostAsJsonAsync("/api/admission/predict", new
        {
            track = "Science",
            score = 300,
            page = 2,
            pageSize = 5
        });
        var second = await page2.Content.ReadFromJsonAsync<ApiEnvelope<PredictData>>();
        Assert.NotNull(second?.Data);
        Assert.Equal(first.Data!.TotalCount, second!.Data!.TotalCount);
        Assert.True(second.Data!.Results.Length > 0);
    }

    [Fact]
    public async Task Predict_ReturnsGroupedResults()
    {
        var response = await _client.PostAsJsonAsync("/api/admission/predict", new
        {
            track = "Science",
            score = 286,
            page = 1,
            pageSize = 10
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<ApiEnvelope<PredictData>>();
        Assert.NotNull(json?.Data);
        Assert.NotEmpty(json!.Data!.Results);
    }

    [Fact]
    public async Task AdminDashboard_RequiresAuth()
    {
        var response = await _client.GetAsync("/api/admin/dashboard");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private record ApiEnvelope<T>(bool Success, T? Data);
    private record ConfigData(string AppName, int CurrentYear, decimal MaximumScore);
    private record PredictData(object[] Results, bool HasMore, int TotalCount);
}
