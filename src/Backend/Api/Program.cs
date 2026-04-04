using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using YepPet.Infrastructure.Auth;
using YepPet.Application;
using YepPet.Api.Endpoints;
using YepPet.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var linkedInLocalConfig = Path.GetFullPath(
    Path.Combine(builder.Environment.ContentRootPath, "..", "..", "..", "config", "linkedin", "yeppet-dev.json"));
var facebookLocalConfig = Path.GetFullPath(
    Path.Combine(builder.Environment.ContentRootPath, "..", "..", "..", "config", "facebook", "yeppet-dev.json"));

builder.Configuration.AddJsonFile(linkedInLocalConfig, optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile(facebookLocalConfig, optional: true, reloadOnChange: true);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();
var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = authOptions.Jwt.Issuer,
            ValidAudience = authOptions.Jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.Jwt.SigningKey)),
            NameClaimType = "sub",
            RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "YepPet API",
        Version = "v1"
    });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("web", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DevelopmentIdentitySeeder>();
    await seeder.SeedAsync();
}

app.UseExceptionHandler();
app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "YepPet API v1");
    options.RoutePrefix = "swagger";
});

app.MapGet("/", () => "Hello World!");
app.MapGet("/health/db", () => Results.Ok(new { status = "configured" }));
app.MapAuthEndpoints();
app.MapNavigationEndpoints();
app.MapAdminEndpoints();
app.MapPlaceEndpoints();
app.MapFavoriteEndpoints();
app.MapUserEndpoints();
app.MapReviewEndpoints();

app.Run();
