using System.Text.Json;
using System.Text.Json.Serialization;
using GameLibraryApi.Mapping;
using GameLibraryApi.Data;
using GameLibraryApi.Exceptions;
using GameLibraryApi.Interfaces;
using GameLibraryApi.Options;
using GameLibraryApi.Repositories;
using GameLibraryApi.Services;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: true);
}

builder.Configuration.AddEnvironmentVariables().AddCommandLine(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    });

builder.Services.Configure<MongoDbOptions>(builder.Configuration.GetSection(MongoDbOptions.SectionName));
builder.Services.Configure<IgdbOptions>(builder.Configuration.GetSection(IgdbOptions.SectionName));
builder.Services.AddAutoMapper(configuration => configuration.AddProfile<GameMappingProfile>());
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<IOptions<MongoDbOptions>>().Value;
    return new MongoClient(options.ConnectionString);
});

builder.Services.AddHttpClient("igdb");
builder.Services.AddSingleton<IIgdbClient, IgdbClient>();
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IUserGameRepository, UserGameRepository>();
builder.Services.AddScoped<IGameLibraryService, GameLibraryService>();

var app = builder.Build();

app.UseExceptionHandler();
await app.Services.GetRequiredService<MongoDbContext>().EnsureIndexesAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapGet("/", () => Results.Ok(new { status = "running" }));
app.MapControllers();

app.Run();
