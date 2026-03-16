using System.Security.Cryptography;
using System.Text;
using blazor_jwt_auth.Client.Models;
using blazor_jwt_auth.Data;
using blazor_jwt_auth.Models;
using blazor_jwt_auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace blazor_jwt_auth.Controllers.v1;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/x-msgpack")]
public class AuthController : Controller
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly JwtSettings _jwtSettings;
    
    public AuthController(IDbContextFactory<ApplicationDbContext> dbContextFactory, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IJwtTokenService tokenService, IOptions<JwtSettings> jwtSettings)
    {
        _dbContextFactory = dbContextFactory;
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = tokenService;
        _jwtSettings = jwtSettings.Value;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);
        if (user == null) return Unauthorized();
        
        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!signInResult.Succeeded) return Unauthorized();

        var accessToken = await _jwtTokenService.CreateAccessToken(user);
        var refreshToken = _jwtTokenService.CreateRefreshToken();
        var hashRefreshToken = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenLifetimeInDays);

        await SetUserRefreshToken(dbContext, user, hashRefreshToken, refreshTokenExpiry, cancellationToken);
        SeRefreshTokenCookies(refreshToken, refreshTokenExpiry);

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenLifetimeInMinutes)
        });
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            Email = request.Email,
            UserName = request.Email,
            EmailConfirmed = true,
        };
        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded) return BadRequest(result.Errors);
        var roleResult = await _userManager.AddToRoleAsync(user, nameof(Roles.User));
        if (!roleResult.Succeeded) return BadRequest(roleResult.Errors);

        return Ok(true);
    }
    
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        Request.Cookies.TryGetValue(_jwtSettings.RefreshCookieName, out var refreshToken);
        if (string.IsNullOrWhiteSpace(refreshToken)) return Unauthorized();
        
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var hashRefreshToken = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.RefreshToken == hashRefreshToken, cancellationToken);
        if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow) return Unauthorized();

        var accessToken = await _jwtTokenService.CreateAccessToken(user);
        var newRefreshToken = _jwtTokenService.CreateRefreshToken();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenLifetimeInDays);
        
        await SetUserRefreshToken(dbContext, user, hashRefreshToken, refreshTokenExpiry, cancellationToken);
        SeRefreshTokenCookies(newRefreshToken, refreshTokenExpiry);

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenLifetimeInMinutes)
        });
    }
    
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(string email, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user == null) return Unauthorized();
        
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        
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
        return Ok(await Task.FromResult("Authorized endpoint successfully fetched"));
    }

    private void SeRefreshTokenCookies(string refreshToken, DateTime expiry)
    {
        Response.Cookies.Append(_jwtSettings.RefreshCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = expiry
        });
    }
    
    private static async Task SetUserRefreshToken(ApplicationDbContext dbContext, ApplicationUser user, string refreshToken, DateTime expiry, CancellationToken cancellationToken)
    {
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = expiry;

        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}