using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.DTOs.Auth;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status200OK, response);
    }

    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] SignupRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _authService.SignupAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("customerSignup")]
    public async Task<IActionResult> CustomerSignup([FromBody] CustomerSignupRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _authService.CustomerSignupAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [Authorize]
    [AcceptVerbs("GET", "POST")]
    [Route("logout")]
    [Route("customer/logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var tokenId = User.FindFirstValue(JwtRegisteredClaimNames.Jti) ?? string.Empty;
        var provider = User.FindFirstValue("provider") ?? "users";
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0";
        var response = await _authService.LogoutAsync(tokenId, provider, ulong.Parse(subject), cancellationToken);
        return Ok(response);
    }
}
