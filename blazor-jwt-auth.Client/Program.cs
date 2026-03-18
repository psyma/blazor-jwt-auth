using blazor_jwt_auth.Client.Data;
using blazor_jwt_auth.Client.Service;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();
builder.Services.AddSingleton<JwtAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthStateProvider>());
builder.Services.AddScoped<AuthHeaderHandler>();
builder.Services.AddScoped<RetryOnUnauthorizedHandler>();
builder.Services.AddHttpClient<IAuthService, AuthService>(client =>
    {
        client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
    })
    .AddHttpMessageHandler<RetryOnUnauthorizedHandler>()
    .AddHttpMessageHandler<AuthHeaderHandler>()
    .RemoveAllLoggers();
builder.Services.AddHttpClient("RefreshClient", client =>
    {
        client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
    })
    .RemoveAllLoggers();

await builder.Build().RunAsync();