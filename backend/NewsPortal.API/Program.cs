using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
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
builder.Services.AddIdentity<NewsPortalUser, IdentityRole<Guid>>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<NewsPortalDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = ".NewsPortal.Admin";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Path = "/";
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
});
builder.Services.AddAuthorization();
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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

var bootstrapEmail = builder.Configuration["AdminBootstrap:Email"];
var bootstrapPassword = builder.Configuration["AdminBootstrap:Password"];
if (!string.IsNullOrWhiteSpace(bootstrapEmail) && !string.IsNullOrWhiteSpace(bootstrapPassword))
{
    using var scope = app.Services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<NewsPortalUser>>();
    if (!await roleManager.RoleExistsAsync("SuperAdmin"))
    {
        var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>("SuperAdmin"));
        if (!roleResult.Succeeded) throw new InvalidOperationException("SuperAdmin rolü oluşturulamadı.");
    }
    var adminUser = await userManager.FindByEmailAsync(bootstrapEmail);
    if (adminUser is null)
    {
        adminUser = new NewsPortalUser { Id = Guid.NewGuid(), UserName = bootstrapEmail, Email = bootstrapEmail, EmailConfirmed = true, DisplayName = "NewsPortal Yöneticisi", IsActive = true, LockoutEnabled = true };
        var userResult = await userManager.CreateAsync(adminUser, bootstrapPassword);
        if (!userResult.Succeeded) throw new InvalidOperationException($"Yönetici hesabı oluşturulamadı: {string.Join("; ", userResult.Errors.Select(x => x.Description))}");
    }
    if (!adminUser.LockoutEnabled)
    {
        var lockoutResult = await userManager.SetLockoutEnabledAsync(adminUser, true);
        if (!lockoutResult.Succeeded) throw new InvalidOperationException("Yönetici hesabı için giriş denemesi koruması etkinleştirilemedi.");
    }
    if (!await userManager.IsInRoleAsync(adminUser, "SuperAdmin"))
    {
        var roleResult = await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
        if (!roleResult.Succeeded) throw new InvalidOperationException("Yönetici hesabına SuperAdmin rolü atanamadı.");
    }
}

if (args.Contains("--seed-admin", StringComparer.OrdinalIgnoreCase)) return;

app.Run();
