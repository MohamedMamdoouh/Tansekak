using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Tansekak.Application.Common;
using Tansekak.Application.DTOs;
using Tansekak.Application.Interfaces;
using Tansekak.Infrastructure.Identity;
using Tansekak.Infrastructure.Services;

namespace Tansekak.Api.Controllers;

[ApiController]
[Route("api")]
public class ConfigController(IConfigService configService) : ControllerBase
{
    [HttpGet("config")]
    public async Task<ActionResult<ApiResponse<ConfigDto>>> Get(CancellationToken cancellationToken)
    {
        var config = await configService.GetConfigAsync(cancellationToken);
        return Ok(ApiResponse<ConfigDto>.Ok(config));
    }
}

[ApiController]
[Route("api/admission")]
public class AdmissionController(IAdmissionPredictionService predictionService) : ControllerBase
{
    [HttpPost("predict")]
    public async Task<ActionResult<ApiResponse<PredictResponseDto>>> Predict(
        [FromBody] PredictRequestDto request, CancellationToken cancellationToken)
    {
        var result = await predictionService.PredictAsync(request, cancellationToken);
        return Ok(ApiResponse<PredictResponseDto>.Ok(result));
    }
}

[ApiController]
[Route("api/admin/auth")]
public class AuthController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager) : ControllerBase
{
    private const string AdministratorRole = "Administrator";

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthUserDto>>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized(ApiResponse<AuthUserDto>.Fail("Invalid credentials."));
        }

        var result = await signInManager.PasswordSignInAsync(user, request.Password, isPersistent: true, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            return Unauthorized(ApiResponse<AuthUserDto>.Fail("Invalid credentials."));
        }

        var authUser = await BuildAuthUserAsync(user);
        if (authUser is null)
        {
            await signInManager.SignOutAsync();
            return Unauthorized(ApiResponse<AuthUserDto>.Fail("Invalid credentials."));
        }

        return Ok(ApiResponse<AuthUserDto>.Ok(authUser));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return Ok(ApiResponse<object>.Ok(new { }, "Logged out successfully."));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<AuthUserDto>>> Me(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Unauthorized(ApiResponse<AuthUserDto>.Fail("Not authenticated."));

        var authUser = await BuildAuthUserAsync(user);
        if (authUser is null) return Unauthorized(ApiResponse<AuthUserDto>.Fail("Not authenticated."));

        return Ok(ApiResponse<AuthUserDto>.Ok(authUser));
    }

    private async Task<AuthUserDto?> BuildAuthUserAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault();
        if (role != AdministratorRole) return null;

        return new AuthUserDto(user.Email!, role);
    }
}

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "Administrator")]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<DashboardDto>>> Get(CancellationToken cancellationToken) =>
        Ok(ApiResponse<DashboardDto>.Ok(await dashboardService.GetDashboardAsync(cancellationToken)));
}

[ApiController]
[Route("api/admin/governorates")]
[Authorize(Roles = "Administrator")]
public class GovernoratesController(IGovernorateService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<GovernorateDto>>>> GetAll(CancellationToken ct) =>
        Ok(ApiResponse<IReadOnlyList<GovernorateDto>>.Ok(await service.GetAllAsync(ct)));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<GovernorateDto>>> GetById(int id, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is null ? NotFound(ApiResponse<GovernorateDto>.Fail("Not found.")) : Ok(ApiResponse<GovernorateDto>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<GovernorateDto>>> Create([FromBody] CreateGovernorateDto dto, CancellationToken ct) =>
        Ok(ApiResponse<GovernorateDto>.Ok(await service.CreateAsync(dto, ct)));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<GovernorateDto>>> Update(int id, [FromBody] UpdateGovernorateDto dto, CancellationToken ct)
    {
        var item = await service.UpdateAsync(id, dto, ct);
        return item is null ? NotFound(ApiResponse<GovernorateDto>.Fail("Not found.")) : Ok(ApiResponse<GovernorateDto>.Ok(item));
    }
}

[ApiController]
[Route("api/admin/universities")]
[Authorize(Roles = "Administrator")]
public class UniversitiesController(IUniversityService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UniversityDto>>>> GetAll(
        [FromQuery] string? search, [FromQuery] int? governorateId, [FromQuery] string? type, CancellationToken ct) =>
        Ok(ApiResponse<IReadOnlyList<UniversityDto>>.Ok(await service.GetAllAsync(search, governorateId, type, ct)));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<UniversityDto>>> GetById(int id, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is null ? NotFound(ApiResponse<UniversityDto>.Fail("Not found.")) : Ok(ApiResponse<UniversityDto>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UniversityDto>>> Create([FromBody] CreateUniversityDto dto, CancellationToken ct) =>
        Ok(ApiResponse<UniversityDto>.Ok(await service.CreateAsync(dto, ct)));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<UniversityDto>>> Update(int id, [FromBody] UpdateUniversityDto dto, CancellationToken ct)
    {
        var item = await service.UpdateAsync(id, dto, ct);
        return item is null ? NotFound(ApiResponse<UniversityDto>.Fail("Not found.")) : Ok(ApiResponse<UniversityDto>.Ok(item));
    }
}

[ApiController]
[Route("api/admin/faculties")]
[Authorize(Roles = "Administrator")]
public class FacultiesController(IFacultyService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FacultyDto>>>> GetAll([FromQuery] string? search, CancellationToken ct) =>
        Ok(ApiResponse<IReadOnlyList<FacultyDto>>.Ok(await service.GetAllAsync(search, ct)));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<FacultyDto>>> GetById(int id, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is null ? NotFound(ApiResponse<FacultyDto>.Fail("Not found.")) : Ok(ApiResponse<FacultyDto>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<FacultyDto>>> Create([FromBody] CreateFacultyDto dto, CancellationToken ct) =>
        Ok(ApiResponse<FacultyDto>.Ok(await service.CreateAsync(dto, ct)));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<FacultyDto>>> Update(int id, [FromBody] UpdateFacultyDto dto, CancellationToken ct)
    {
        var item = await service.UpdateAsync(id, dto, ct);
        return item is null ? NotFound(ApiResponse<FacultyDto>.Fail("Not found.")) : Ok(ApiResponse<FacultyDto>.Ok(item));
    }
}

[ApiController]
[Route("api/admin/university-faculties")]
[Authorize(Roles = "Administrator")]
public class UniversityFacultiesController(IUniversityFacultyService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UniversityFacultyDto>>>> GetAll(
        [FromQuery] string? search, [FromQuery] int? universityId, [FromQuery] int? facultyId, CancellationToken ct) =>
        Ok(ApiResponse<IReadOnlyList<UniversityFacultyDto>>.Ok(await service.GetAllAsync(search, universityId, facultyId, ct)));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<UniversityFacultyDto>>> GetById(int id, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is null ? NotFound(ApiResponse<UniversityFacultyDto>.Fail("Not found.")) : Ok(ApiResponse<UniversityFacultyDto>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UniversityFacultyDto>>> Create([FromBody] CreateUniversityFacultyDto dto, CancellationToken ct) =>
        Ok(ApiResponse<UniversityFacultyDto>.Ok(await service.CreateAsync(dto, ct)));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<UniversityFacultyDto>>> Update(int id, [FromBody] UpdateUniversityFacultyDto dto, CancellationToken ct)
    {
        var item = await service.UpdateAsync(id, dto, ct);
        return item is null ? NotFound(ApiResponse<UniversityFacultyDto>.Fail("Not found.")) : Ok(ApiResponse<UniversityFacultyDto>.Ok(item));
    }
}

[ApiController]
[Route("api/admin/admission-years")]
[Authorize(Roles = "Administrator")]
public class AdmissionYearsController(IAdmissionYearService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AdmissionYearDto>>>> GetAll(CancellationToken ct) =>
        Ok(ApiResponse<IReadOnlyList<AdmissionYearDto>>.Ok(await service.GetAllAsync(ct)));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<AdmissionYearDto>>> GetById(int id, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is null ? NotFound(ApiResponse<AdmissionYearDto>.Fail("Not found.")) : Ok(ApiResponse<AdmissionYearDto>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AdmissionYearDto>>> Create([FromBody] CreateAdmissionYearDto dto, CancellationToken ct) =>
        Ok(ApiResponse<AdmissionYearDto>.Ok(await service.CreateAsync(dto, ct)));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<AdmissionYearDto>>> Update(int id, [FromBody] UpdateAdmissionYearDto dto, CancellationToken ct)
    {
        var item = await service.UpdateAsync(id, dto, ct);
        return item is null ? NotFound(ApiResponse<AdmissionYearDto>.Fail("Not found.")) : Ok(ApiResponse<AdmissionYearDto>.Ok(item));
    }

    [HttpPatch("{id:int}/publish")]
    public async Task<IActionResult> Publish(int id, CancellationToken ct)
    {
        var ok = await service.PublishAsync(id, ct);
        return ok ? Ok(ApiResponse<object>.Ok(new { }, "Year published successfully.")) : NotFound(ApiResponse<object>.Fail("Not found."));
    }
}

[ApiController]
[Route("api/admin/admission-cutoffs")]
[Authorize(Roles = "Administrator")]
public class AdmissionCutoffsController(IAdmissionCutoffService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResultDto<AdmissionCutoffDto>>>> GetAll(
        [FromQuery] int? yearId, [FromQuery] string? search, [FromQuery] string? track,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default) =>
        Ok(ApiResponse<PagedResultDto<AdmissionCutoffDto>>.Ok(
            await service.GetPagedAsync(yearId, search, track, page, pageSize, ct)));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<AdmissionCutoffDto>>> GetById(int id, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is null ? NotFound(ApiResponse<AdmissionCutoffDto>.Fail("Not found.")) : Ok(ApiResponse<AdmissionCutoffDto>.Ok(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AdmissionCutoffDto>>> Create([FromBody] CreateAdmissionCutoffDto dto, CancellationToken ct) =>
        Ok(ApiResponse<AdmissionCutoffDto>.Ok(await service.CreateAsync(dto, ct)));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<AdmissionCutoffDto>>> Update(int id, [FromBody] UpdateAdmissionCutoffDto dto, CancellationToken ct)
    {
        var item = await service.UpdateAsync(id, dto, ct);
        return item is null ? NotFound(ApiResponse<AdmissionCutoffDto>.Fail("Not found.")) : Ok(ApiResponse<AdmissionCutoffDto>.Ok(item));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var ok = await service.DeleteAsync(id, ct);
        return ok ? Ok(ApiResponse<object>.Ok(new { }, "Deleted successfully.")) : NotFound(ApiResponse<object>.Fail("Not found."));
    }
}

[ApiController]
[Route("api/thanaweya-results")]
public class ThanaweyaResultsController(IStudentResultService studentResultService) : ControllerBase
{
    [HttpGet("{seatingNo}")]
    public async Task<ActionResult<ApiResponse<StudentResultDto>>> Lookup(string seatingNo, CancellationToken ct)
    {
        var result = await studentResultService.LookupBySeatingNoAsync(seatingNo, ct);
        return result is null
            ? NotFound(ApiResponse<StudentResultDto>.Fail("No result found for this seating number."))
            : Ok(ApiResponse<StudentResultDto>.Ok(result));
    }
}

[ApiController]
[Route("api/admin/admission-years/{yearId:int}/import-results")]
[Authorize(Roles = "Administrator")]
public class StudentResultImportController(
    StudentResultImportJobQueue jobQueue,
    R2ImportStorageService r2Storage) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(R2ImportStorageService.MaxFileSizeBytes)]
    public async Task<ActionResult<ApiResponse<ImportJobStartedDto>>> Import(int yearId, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<ImportJobStartedDto>.Fail("File is required."));

        var ext = Path.GetExtension(file.FileName);
        if (!ext.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<ImportJobStartedDto>.Fail("Only .xlsx files are supported."));

        var tempPath = Path.Combine(Path.GetTempPath(), $"tansekak-import-{Guid.NewGuid():N}.xlsx");
        await using (var output = System.IO.File.Create(tempPath))
            await file.CopyToAsync(output, ct);

        var job = jobQueue.Enqueue(yearId, tempPath, file.FileName);
        return Accepted(ApiResponse<ImportJobStartedDto>.Ok(
            new ImportJobStartedDto(job.JobId, job.Status),
            "Import started. Poll /api/admin/import-jobs/{jobId} for status."));
    }

    [HttpPost("upload-url")]
    public ActionResult<ApiResponse<ImportUploadUrlDto>> CreateUploadUrl(
        int yearId,
        [FromBody] CreateImportUploadUrlDto dto)
    {
        if (!r2Storage.IsConfigured)
            return StatusCode(503, ApiResponse<ImportUploadUrlDto>.Fail(
                "Direct upload is not configured. Set R2 environment variables on the server."));

        try
        {
            var upload = r2Storage.CreatePresignedUploadUrl(yearId, dto.FileName, dto.TotalSize);
            return Ok(ApiResponse<ImportUploadUrlDto>.Ok(upload, "Upload URL created."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<ImportUploadUrlDto>.Fail(ex.Message));
        }
    }

    [HttpPost("from-storage")]
    public async Task<ActionResult<ApiResponse<ImportJobStartedDto>>> ImportFromStorage(
        int yearId,
        [FromBody] ImportFromStorageDto dto,
        CancellationToken ct)
    {
        if (!r2Storage.IsConfigured)
            return StatusCode(503, ApiResponse<ImportJobStartedDto>.Fail(
                "Direct upload is not configured. Set R2 environment variables on the server."));

        try
        {
            await r2Storage.ValidateObjectAsync(dto.ObjectKey, yearId, ct);
            var job = jobQueue.EnqueueFromStorage(yearId, dto.ObjectKey, dto.FileName);
            return Accepted(ApiResponse<ImportJobStartedDto>.Ok(
                new ImportJobStartedDto(job.JobId, job.Status),
                "Import started. Poll /api/admin/import-jobs/{jobId} for status."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<ImportJobStartedDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ImportJobStartedDto>.Fail(ex.Message));
        }
    }
}

[ApiController]
[Route("api/admin/import-jobs")]
[Authorize(Roles = "Administrator")]
public class ImportJobsController(StudentResultImportJobQueue jobQueue) : ControllerBase
{
    [HttpGet("{jobId:guid}")]
    public ActionResult<ApiResponse<ImportJobStatusDto>> Get(Guid jobId)
    {
        var job = jobQueue.GetJob(jobId);
        if (job is null)
            return NotFound(ApiResponse<ImportJobStatusDto>.Fail("Import job not found."));

        return Ok(ApiResponse<ImportJobStatusDto>.Ok(
            new ImportJobStatusDto(job.JobId, job.Status, job.Result, job.Message)));
    }
}

[ApiController]
[Route("api/admin/admission-years/{yearId:int}/import")]
[Authorize(Roles = "Administrator")]
public class ImportController(IImportService importService) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(10_485_760)]
    public async Task<ActionResult<ApiResponse<ImportResultDto>>> Import(int yearId, IFormFile file, [FromForm] string track, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<ImportResultDto>.Fail("File is required."));

        if (string.IsNullOrWhiteSpace(track))
            return BadRequest(ApiResponse<ImportResultDto>.Fail("Track is required."));

        var ext = Path.GetExtension(file.FileName);
        if (!ext.Equals(".md", StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<ImportResultDto>.Fail("Only .md files are supported."));

        await using var stream = file.OpenReadStream();
        var result = await importService.ImportAsync(yearId, track, stream, file.FileName, ct);
        return result.Success
            ? Ok(ApiResponse<ImportResultDto>.Ok(result, result.Message))
            : BadRequest(ApiResponse<ImportResultDto>.Fail(result.Message, result.Errors?.Select(e => new ApiError
            {
                Field = e.Column,
                Message = e.Message,
                RowNumber = e.RowNumber,
                ErrorCode = e.ErrorCode
            }).ToList()));
    }
}
