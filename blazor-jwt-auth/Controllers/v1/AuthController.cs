

using blazor_jwt_auth.Client.Models;
using blazor_jwt_auth.Data;
using blazor_jwt_auth.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace blazor_jwt_auth.Controllers.v1;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : Controller
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;
    
    public AuthController(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService tokenService,
        IConfiguration configuration)
    {
        _dbContextFactory = dbContextFactory;
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = tokenService;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null) return Unauthorized();

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!result.Succeeded) return Unauthorized();

        var accessToken = await _jwtTokenService.CreateAccessToken(user);
        var refreshToken = _jwtTokenService.CreateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(
            int.Parse(_configuration["Jwt:RefreshTokenDays"]!));

        await _userManager.UpdateAsync(user);

        // Optional: cookie for web
        Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = user.RefreshTokenExpiryTime
        });

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(
                int.Parse(_configuration["Jwt:AccessTokenMinutes"]!))
        });
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            Email = request.Email,
            UserName = request.Email,
            EmailConfirmed = true,
        };
        
        var result = await _userManager.CreateAsync(user, request.Password);
        return Ok(result.Succeeded);
    }
    
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest? request)
    {
        var refreshToken = request?.RefreshToken;

        if (string.IsNullOrWhiteSpace(refreshToken))
            Request.Cookies.TryGetValue("refreshToken", out refreshToken);

        if (string.IsNullOrWhiteSpace(refreshToken))
            return Unauthorized();

        var user = _userManager.Users.FirstOrDefault(u => u.RefreshToken == refreshToken);
        if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            return Unauthorized();

        var newAccessToken = await _jwtTokenService.CreateAccessToken(user);
        var newRefreshToken = _jwtTokenService.CreateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(
            int.Parse(_configuration["Jwt:RefreshTokenDays"]!));

        await _userManager.UpdateAsync(user);

        Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = user.RefreshTokenExpiryTime
        });

        return Ok(new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(
                int.Parse(_configuration["Jwt:AccessTokenMinutes"]!))
        });
    }
    
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return Unauthorized();
        
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        await _userManager.UpdateAsync(user);
        
        return Ok();
    }
    
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var result = new
        {
            Name = User.Identity?.Name,
            Claims = User.Claims.Select(c => new { c.Type, c.Value })
        };
        
        return Ok(await Task.FromResult(result));
    }
    
    [Authorize]
    [HttpGet("test")]
    public async Task<IActionResult> Test()
    {
        return Ok(await Task.FromResult("Test endpoint which is authorized"));
    }
}