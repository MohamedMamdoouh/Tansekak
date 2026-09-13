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
[Route("api/admin/auth")]
public class AuthController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager) : ControllerBase
{

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthUserDto>>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized(ApiResponse<AuthUserDto>.Fail("Invalid credentials."));
        }

        var result = await signInManager.PasswordSignInAsync(user, request.Password, isPersistent: true, lockoutOnFailure: true);
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
        if (role != Roles.Administrator) return null;

        return new AuthUserDto(user.Email!, role);
    }
}