using System.Text;
using blazor_jwt_auth.Client.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using blazor_jwt_auth.Client.Service;
using blazor_jwt_auth.Components;
using blazor_jwt_auth.Components.Account;
using blazor_jwt_auth.Data;
using blazor_jwt_auth.Models;
using blazor_jwt_auth.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();
builder.Services.AddAuthorization();

builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), mySqlOptions =>
    {
        mySqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
    });
    options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
}, poolSize: 2);
builder.Services.AddScoped<ApplicationDbContext>(p => p.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.SignIn.RequireConfirmedEmail = true;
    })
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? throw new InvalidOperationException("Jwt settings not found.");

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings")); 

// Client
builder.Services.AddHttpClient<IAuthService, AuthService>().AddHttpMessageHandler<AuthHeaderHandler>().RemoveAllLoggers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(blazor_jwt_auth.Client._Imports).Assembly);

// Add additional endpoints required by the Identity /Account Razor components.
// app.MapAdditionalIdentityEndpoints();

app.MapControllers();

app.Run();