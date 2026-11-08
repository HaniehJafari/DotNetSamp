using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);


var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetSection("jwt:key").Value ?? throw new Exception("")));

var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "MyIssuer",
            ValidAudience = "MyAudience",
            IssuerSigningKey = securityKey,
            ClockSkew = TimeSpan.FromSeconds(2)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();


app.UseAuthentication();
app.UseAuthorization();


app.MapPost("/login", (UserCredential credential) =>
{
   var secKt = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(""));
    var credentials = new SigningCredentials(secKt, SecurityAlgorithms.HmacSha256);

   
    
    if (credential.Username == "admin" && credential.Password == "password")
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, credential.Username),
            new Claim(ClaimTypes.Role, "Admin")
        };
     

        var token = new JwtSecurityToken(
            issuer: "MyIssuer",
            audience: "MyAudience",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return Results.Ok(new { token = tokenString });
    }
    return Results.Unauthorized();
});


app.MapGet("/secure-data", [Authorize] (ClaimsPrincipal user) =>
{
    
    return Results.Ok(new
    {
        data = "This is a secure data",
        user = user.Identity?.Name
    });
});

app.Run();

record UserCredential(int UserId,string Username, string Password);