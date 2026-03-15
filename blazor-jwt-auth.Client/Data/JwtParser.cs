using System.Security.Claims;
using System.Text.Json;

namespace blazor_jwt_auth.Client.Data;

public static class JwtParser
{
    public static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);

        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes)
                            ?? new Dictionary<string, object>();

        var claims = new List<Claim>();

        foreach (var kvp in keyValuePairs)
        {
            if (kvp.Key == "role" || kvp.Key == ClaimTypes.Role)
            {
                if (kvp.Value is JsonElement roleElement)
                {
                    if (roleElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var role in roleElement.EnumerateArray())
                        {
                            claims.Add(new Claim(ClaimTypes.Role, role.GetString() ?? string.Empty));
                        }
                    }
                    else
                    {
                        claims.Add(new Claim(ClaimTypes.Role, roleElement.GetString() ?? string.Empty));
                    }
                }
                else
                {
                    claims.Add(new Claim(ClaimTypes.Role, kvp.Value?.ToString() ?? string.Empty));
                }
            }
            else
            {
                claims.Add(new Claim(kvp.Key, kvp.Value?.ToString() ?? string.Empty));
            }
        }

        return claims;
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        return Convert.FromBase64String(base64);
    }

    public static bool IsTokenExpired(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);

        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes)
                            ?? new Dictionary<string, object>();

        if (!keyValuePairs.TryGetValue("exp", out var expValue))
            return true;

        long expUnix;

        if (expValue is JsonElement expElement && expElement.ValueKind == JsonValueKind.Number)
        {
            expUnix = expElement.GetInt64();
        }
        else if (!long.TryParse(expValue?.ToString(), out expUnix))
        {
            return true;
        }

        var expiry = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
        return expiry <= DateTime.UtcNow;
    }
}