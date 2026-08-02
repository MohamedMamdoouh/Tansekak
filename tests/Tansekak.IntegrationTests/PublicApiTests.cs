using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Tansekak.Domain.Entities;
using Tansekak.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Tansekak.Infrastructure.Persistence;
using Tansekak.Infrastructure.Seeding;

namespace Tansekak.IntegrationTests;

public class PublicApiTests : IClassFixture<TansekakWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TansekakWebApplicationFactory _factory;

    public PublicApiTests(TansekakWebApplicationFactory factory)
    {
        _factory = factory;
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
        Assert.Equal(2026, json.Data!.CurrentYear);
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
    public async Task Predict_Science_DoesNotReturnEngineering()
    {
        var response = await _client.PostAsJsonAsync("/api/admission/predict", new
        {
            track = "Science",
            score = 300,
            page = 1,
            pageSize = 100
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<ApiEnvelope<PredictData>>();
        Assert.NotNull(json?.Data);
        Assert.DoesNotContain(
            json!.Data!.Results,
            r => r.Faculty.NameAr == "هندسة");
    }

    [Fact]
    public async Task Predict_Mathematics_ReturnsEngineering_WhenScoreQualifies()
    {
        var response = await _client.PostAsJsonAsync("/api/admission/predict", new
        {
            track = "Mathematics",
            score = 300,
            page = 1,
            pageSize = 100
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<ApiEnvelope<PredictData>>();
        Assert.NotNull(json?.Data);
        Assert.Contains(
            json!.Data!.Results,
            r => r.Faculty.NameAr == "هندسة");
    }

    [Fact]
    public async Task Predict_Mathematics_ReturnsEngineering_AtExactCutoff()
    {
        var response = await _client.PostAsJsonAsync("/api/admission/predict", new
        {
            track = "Mathematics",
            score = 295.0,
            page = 1,
            pageSize = 100
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<ApiEnvelope<PredictData>>();
        Assert.NotNull(json?.Data);
        Assert.Contains(
            json!.Data!.Results,
            r => r.Faculty.NameAr == "هندسة" && r.University.NameAr == "جامعة عين شمس");
    }

    [Fact]
    public async Task Predict_Mathematics_ReturnsEngineering_AfterAllowedTracksSync()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var engineering = await db.Faculties.SingleAsync(f => f.Id == 16);
            engineering.AllowedTracks = [];
            await db.SaveChangesAsync();
        }

        var blocked = await _client.PostAsJsonAsync("/api/admission/predict", new
        {
            track = "Mathematics",
            score = 300,
            page = 1,
            pageSize = 100
        });
        var blockedJson = await blocked.Content.ReadFromJsonAsync<ApiEnvelope<PredictData>>();
        Assert.NotNull(blockedJson?.Data);
        Assert.DoesNotContain(
            blockedJson!.Data!.Results,
            r => r.Faculty.NameAr == "هندسة");

        using (var scope = _factory.Services.CreateScope())
        {
            var sync = scope.ServiceProvider.GetRequiredService<FacultyAllowedTracksSynchronizer>();
            await sync.SyncAsync();
        }

        var restored = await _client.PostAsJsonAsync("/api/admission/predict", new
        {
            track = "Mathematics",
            score = 300,
            page = 1,
            pageSize = 100
        });
        var restoredJson = await restored.Content.ReadFromJsonAsync<ApiEnvelope<PredictData>>();
        Assert.NotNull(restoredJson?.Data);
        Assert.Contains(
            restoredJson!.Data!.Results,
            r => r.Faculty.NameAr == "هندسة");
    }

    [Fact]
    public async Task Import_RejectsEngineeringForScienceTrack()
    {
        using var authClient = await CreateAuthenticatedClientAsync();

        const string markdown = """
            | الكلية | الحد الأدنى |
            | --- | --- |
            | هندسة عين شمس | 295.0 |
            """;

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("Science"), "track");
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(markdown)), "file", "science.md");

        var yearId = await GetCurrentYearIdAsync();
        var response = await authClient.PostAsync($"/api/admin/admission-years/{yearId}/import", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<ApiEnvelope<ImportData>>();
        Assert.NotNull(json);
        Assert.False(json!.Success);
        Assert.Contains(json.Errors ?? [], e => e.ErrorCode == "TRACK_NOT_ALLOWED");
    }

    [Fact]
    public async Task AdminDashboard_RequiresAuth()
    {
        var response = await _client.GetAsync("/api/admin/dashboard");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ThanaweyaResult_ReturnsNotFound_WhenMissing()
    {
        var response = await _client.GetAsync("/api/thanaweya-results/0000000");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ThanaweyaResult_ReturnsResult_WhenExists()
    {
        const string seatingNo = "9876543";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var currentYear = db.AdmissionYears.Single(x => x.IsCurrent);
            db.StudentResults.Add(new StudentResult
            {
                AdmissionYearId = currentYear.Id,
                SeatingNo = seatingNo,
                ArabicName = "محمد أحمد",
                TotalDegree = 295.5m,
                StudentCaseDesc = "ناجح - علمي رياضة",
                Track = AcademicTrack.Mathematics
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/thanaweya-results/{seatingNo}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<ApiEnvelope<StudentResultData>>();
        Assert.NotNull(json);
        Assert.True(json!.Success);
        Assert.Equal(seatingNo, json.Data!.SeatingNo);
        Assert.Equal("محمد أحمد", json.Data.ArabicName);
        Assert.Equal(295.5m, json.Data.TotalDegree);
        Assert.Equal("ناجح - علمي رياضة", json.Data.StudentCaseDesc);
        Assert.Equal(2026, json.Data.Year);
        Assert.Equal("Mathematics", json.Data.Track);
    }

    [Fact]
    public async Task ThanaweyaResult_InfersTrackFromCaseDesc_WhenTrackMissing()
    {
        const string seatingNo = "1122334";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var currentYear = db.AdmissionYears.Single(x => x.IsCurrent);
            db.StudentResults.Add(new StudentResult
            {
                AdmissionYearId = currentYear.Id,
                SeatingNo = seatingNo,
                ArabicName = "سارة علي",
                TotalDegree = 280m,
                StudentCaseDesc = "ناجح - علمي علوم"
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/thanaweya-results/{seatingNo}");
        var json = await response.Content.ReadFromJsonAsync<ApiEnvelope<StudentResultData>>();

        Assert.NotNull(json?.Data);
        Assert.Equal("Science", json!.Data!.Track);
    }

    [Fact]
    public async Task ThanaweyaResult_ReturnsUniqueTrackRank_WithSeatingTieBreak()
    {
        const string targetSeatingNo = "2003";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var currentYear = db.AdmissionYears.Single(x => x.IsCurrent);
            await ClearStudentResultsAsync(db, currentYear.Id);
            db.StudentResults.AddRange(
                new StudentResult
                {
                    AdmissionYearId = currentYear.Id,
                    SeatingNo = "2001",
                    ArabicName = "أحمد 1",
                    TotalDegree = 300m,
                    StudentCaseDesc = "ناجح - علمي رياضة",
                    Track = AcademicTrack.Mathematics
                },
                new StudentResult
                {
                    AdmissionYearId = currentYear.Id,
                    SeatingNo = "2002",
                    ArabicName = "أحمد 2",
                    TotalDegree = 295m,
                    StudentCaseDesc = "ناجح - علمي رياضة",
                    Track = AcademicTrack.Mathematics
                },
                new StudentResult
                {
                    AdmissionYearId = currentYear.Id,
                    SeatingNo = targetSeatingNo,
                    ArabicName = "أحمد 3",
                    TotalDegree = 295m,
                    StudentCaseDesc = "ناجح - علمي رياضة",
                    Track = AcademicTrack.Mathematics
                },
                new StudentResult
                {
                    AdmissionYearId = currentYear.Id,
                    SeatingNo = "2004",
                    ArabicName = "أحمد 4",
                    TotalDegree = 290m,
                    StudentCaseDesc = "ناجح - علمي رياضة",
                    Track = AcademicTrack.Mathematics
                });
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/thanaweya-results/{targetSeatingNo}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("trackRank", body, StringComparison.OrdinalIgnoreCase);

        var json = await response.Content.ReadFromJsonAsync<ApiEnvelope<StudentResultData>>();
        Assert.NotNull(json?.Data);
        Assert.Equal(3, json!.Data!.TrackRank);
        Assert.Equal(4, json.Data.TrackTotalStudents);
    }

    [Fact]
    public async Task ThanaweyaResult_ScopesRankToTrackOnly()
    {
        const string targetSeatingNo = "3001";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var currentYear = db.AdmissionYears.Single(x => x.IsCurrent);
            await ClearStudentResultsAsync(db, currentYear.Id);
            db.StudentResults.AddRange(
                new StudentResult
                {
                    AdmissionYearId = currentYear.Id,
                    SeatingNo = targetSeatingNo,
                    ArabicName = "طالب علوم",
                    TotalDegree = 280m,
                    StudentCaseDesc = "ناجح - علمي علوم",
                    Track = AcademicTrack.Science
                },
                new StudentResult
                {
                    AdmissionYearId = currentYear.Id,
                    SeatingNo = "3002",
                    ArabicName = "طالب رياضة",
                    TotalDegree = 310m,
                    StudentCaseDesc = "ناجح - علمي رياضة",
                    Track = AcademicTrack.Mathematics
                });
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/thanaweya-results/{targetSeatingNo}");
        var json = await response.Content.ReadFromJsonAsync<ApiEnvelope<StudentResultData>>();

        Assert.NotNull(json?.Data);
        Assert.Equal(1, json!.Data!.TrackRank);
        Assert.Equal(1, json.Data.TrackTotalStudents);
    }

    [Fact]
    public async Task ThanaweyaResult_OmitsRank_WhenTrackUnknown()
    {
        const string seatingNo = "4001";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var currentYear = db.AdmissionYears.Single(x => x.IsCurrent);
            db.StudentResults.Add(new StudentResult
            {
                AdmissionYearId = currentYear.Id,
                SeatingNo = seatingNo,
                ArabicName = "طالب بدون شعبة",
                TotalDegree = 250m,
                StudentCaseDesc = "ناجح"
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/thanaweya-results/{seatingNo}");
        var json = await response.Content.ReadFromJsonAsync<ApiEnvelope<StudentResultData>>();

        Assert.NotNull(json?.Data);
        Assert.Null(json!.Data!.Track);
        Assert.Null(json.Data.TrackRank);
        Assert.Null(json.Data.TrackTotalStudents);
    }

    [Fact]
    public async Task ThanaweyaResult_InfersTrackAndRank_FromSeatingNoPattern()
    {
        const string seatingNo = "2700100";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var currentYear = db.AdmissionYears.Single(x => x.IsCurrent);
            await ClearStudentResultsAsync(db, currentYear.Id);
            db.StudentResults.AddRange(
                new StudentResult
                {
                    AdmissionYearId = currentYear.Id,
                    SeatingNo = "2700101",
                    ArabicName = "طالب 1",
                    TotalDegree = 300m,
                    StudentCaseDesc = "ناجح دور أول"
                },
                new StudentResult
                {
                    AdmissionYearId = currentYear.Id,
                    SeatingNo = seatingNo,
                    ArabicName = "طالب 2",
                    TotalDegree = 295m,
                    StudentCaseDesc = "ناجح دور أول"
                },
                new StudentResult
                {
                    AdmissionYearId = currentYear.Id,
                    SeatingNo = "2400100",
                    ArabicName = "طالب رياضة",
                    TotalDegree = 310m,
                    StudentCaseDesc = "ناجح دور أول"
                });
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/thanaweya-results/{seatingNo}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<ApiEnvelope<StudentResultData>>();
        Assert.NotNull(json?.Data);
        Assert.Equal("Science", json!.Data!.Track);
        Assert.Equal(2, json.Data.TrackRank);
        Assert.Equal(2, json.Data.TrackTotalStudents);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var loginResponse = await client.PostAsJsonAsync("/api/admin/auth/login", new
        {
            email = "admin@tansekak.local",
            password = "Admin@12345"
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        return client;
    }

    private async Task<int> GetCurrentYearIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return db.AdmissionYears.Single(x => x.IsCurrent).Id;
    }

    private static async Task ClearStudentResultsAsync(AppDbContext db, int admissionYearId)
    {
        var existing = await db.StudentResults
            .Where(x => x.AdmissionYearId == admissionYearId)
            .ToListAsync();
        db.StudentResults.RemoveRange(existing);
        await db.SaveChangesAsync();
    }

    private record ApiEnvelope<T>(bool Success, T? Data, ApiError[]? Errors = null);
    private record ApiError(string Field, string Message, int? RowNumber, string ErrorCode);
    private record ConfigData(string AppName, int CurrentYear, decimal MaximumScore);
    private record PredictData(PredictResultItem[] Results, bool HasMore, int TotalCount);
    private record PredictResultItem(NamedEntityData University, NamedEntityData Faculty);
    private record NamedEntityData(string NameAr);
    private record ImportData(bool Success, string Message);
    private record StudentResultData(
        string SeatingNo,
        string ArabicName,
        decimal TotalDegree,
        string StudentCaseDesc,
        int Year,
        string? Track,
        int? TrackRank,
        int? TrackTotalStudents);
}
