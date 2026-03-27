using YepPet.Application;
using YepPet.Api.Endpoints;
using YepPet.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();
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

app.UseExceptionHandler();
app.UseCors("web");
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "YepPet API v1");
    options.RoutePrefix = "swagger";
});

app.MapGet("/", () => "Hello World!");
app.MapGet("/health/db", () => Results.Ok(new { status = "configured" }));
app.MapPlaceEndpoints();
app.MapFavoriteEndpoints();
app.MapUserEndpoints();
app.MapReviewEndpoints();

app.Run();
