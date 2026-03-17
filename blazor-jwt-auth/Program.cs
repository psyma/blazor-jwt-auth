using System.IO.Compression;
using System.Text;
using blazor_jwt_auth.Client.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using blazor_jwt_auth.Client.Service;
using blazor_jwt_auth.Components;
using blazor_jwt_auth.Components.Account;
using blazor_jwt_auth.Data;
using blazor_jwt_auth.Email;
using blazor_jwt_auth.Models;
using blazor_jwt_auth.Services;
using MessagePack;
using MessagePack.AspNetCoreMvcFormatter;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
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
builder.Services.AddControllers(options =>
{
    options.InputFormatters.Insert(0, new MessagePackInputFormatter(MessagePackSerializerOptions.Standard));
    options.OutputFormatters.Insert(0, new MessagePackOutputFormatter(MessagePackSerializerOptions.Standard)); 
});

//builder.Services.AddAuthentication(options =>
//    {
//        options.DefaultScheme = IdentityConstants.ApplicationScheme;
//        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
//    })
//    
//    .AddIdentityCookies();
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
        options.Tokens.ProviderMap.Add("CustomEmail", new TokenProviderDescriptor(typeof(CustomEmailTokenProvider<ApplicationUser>)));
        options.Tokens.EmailConfirmationTokenProvider = "CustomEmail";
    })
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();
builder.Services.AddTransient<CustomEmailTokenProvider<ApplicationUser>>();
builder.Services.AddSingleton<ICustomEmailSender<ApplicationUser>, CustomEmailSender>();

var googleSettings = builder.Configuration.GetSection("GoogleSettings").Get<GoogleSettings>() ?? throw new InvalidOperationException("Google settings not found.");
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? throw new InvalidOperationException("Jwt settings not found.");
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddCookie(IdentityConstants.ExternalScheme, options =>
    {
        options.Cookie.Name = googleSettings.CookieName;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
        options.SlidingExpiration = false;
    })
    .AddGoogle(options =>
    {
        options.ClientId = googleSettings.Id;
        options.ClientSecret = googleSettings.Secret;
        options.CallbackPath = googleSettings.CallbackPath;
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.ClaimActions.MapJsonKey("picture", "picture", "url");
        options.SignInScheme = IdentityConstants.ExternalScheme;
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

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat([
        "text/plain",
        "application/json",
        "application/x-msgpack",
        "application/octet-stream"
    ]);
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options => { options.Level = CompressionLevel.Optimal; });
builder.Services.Configure<GzipCompressionProviderOptions>(options => { options.Level = CompressionLevel.Optimal; });

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings")); 
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// Client
builder.Services.AddHttpClient<IAuthService, AuthService>().AddHttpMessageHandler<AuthHeaderHandler>().RemoveAllLoggers();

var app = builder.Build();

// Seeding
using var scope = app.Services.CreateScope();
var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
await using var context = await factory.CreateDbContextAsync();
if (!context.DataSeedStates.Any())
{
    await context.Database.OpenConnectionAsync();
    try
    {
        var roles = new List<IdentityRole<int>>
        {
            new()
            {
                Id = (int)Roles.Administrator,
                Name = nameof(Roles.Administrator),
                NormalizedName = nameof(Roles.Administrator).ToUpper(),
                ConcurrencyStamp = null
            },
            new()
            {
                Id = (int)Roles.User,
                Name = nameof(Roles.User),
                NormalizedName = nameof(Roles.User).ToUpper(),
                ConcurrencyStamp = null
            }
        };

        await context.DataSeedStates.AddAsync(new DataSeedState { CreatedAt = DateTime.UtcNow });
        await context.Roles.AddRangeAsync(roles);
        await context.SaveChangesAsync();
    }
    finally
    {
        await context.Database.CloseConnectionAsync();
    }
}

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

app.UseResponseCompression();
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