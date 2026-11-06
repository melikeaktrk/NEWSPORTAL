using Microsoft.EntityFrameworkCore;
using NewsPortal.Infrastructure.Persistence;
using NewsPortal.Application.ExchangeRates;
using NewsPortal.Application.Weather;
using NewsPortal.Infrastructure.Integrations;
using NewsPortal.Infrastructure.Feeds;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddMemoryCache();
builder.Services.AddDbContextFactory<NewsPortalDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHttpClient<IWeatherService, OpenMeteoWeatherService>(client => client.Timeout = TimeSpan.FromSeconds(12));
builder.Services.AddHttpClient<IExchangeRateService, FrankfurterExchangeRateService>(client => client.Timeout = TimeSpan.FromSeconds(12));
builder.Services.AddHttpClient("rss", client => client.Timeout = TimeSpan.FromSeconds(12))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddSingleton<RssNewsService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<RssNewsService>());
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:3000"])
        .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();
