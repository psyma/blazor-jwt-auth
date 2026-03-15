dotnet ef migrations add Initial
dotnet ef database update

{
  "ConnectionStrings": {
    "DefaultConnection": "Server=0.0.0.0;Port=3306;Database=dbname;User=dbuser;Password=dbpassword;"
  },
  "Jwt": {
    "Issuer": "http://localhost:5128",
    "Audience": "http://localhost:5128",
    "Key": "32 characters",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 30
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}

