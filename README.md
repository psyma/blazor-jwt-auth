```
# Migrations

dotnet ef migrations add Initial 
dotnet ef database update
```

```
# appsettings.json

{
  "ConnectionStrings": {
    "DefaultConnection": "Server=0.0.0.0;Port=3306;Database=dbname;User=dbuser;Password=dbpassword;"
  },
  "JwtSettings": {
    "Issuer": "http://localhost:5128",
    "Audience": "http://localhost:5128",
    "Secret": "32 characters",
    "RefreshCookieName": "random cookie name",
    "AccessTokenLifetimeInMinutes": 15,
    "RefreshTokenLifetimeInDays": 30
  },
  "EmailSettings": {
    "Host": "",
    "Port": 465,
    "Username": "",
    "Password": ""
  },
  Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```
