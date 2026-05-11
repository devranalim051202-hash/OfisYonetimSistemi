using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OfisYonetimSistemi.Models;
using OfisYonetimSistemi.Services;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    var messages = options.ModelBindingMessageProvider;

    messages.SetAttemptedValueIsInvalidAccessor((value, fieldName) => $"{fieldName} alanına girilen değer geçerli değil.");
    messages.SetMissingBindRequiredValueAccessor(fieldName => $"{fieldName} alanı zorunludur.");
    messages.SetMissingKeyOrValueAccessor(() => "Zorunlu alan değeri eksik.");
    messages.SetMissingRequestBodyRequiredValueAccessor(() => "İstek gövdesi boş olamaz.");
    messages.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"Girilen değer geçerli değil: {value}");
    messages.SetNonPropertyUnknownValueIsInvalidAccessor(() => "Girilen değer geçerli değil.");
    messages.SetNonPropertyValueMustBeANumberAccessor(() => "Lütfen geçerli bir sayı girin.");
    messages.SetUnknownValueIsInvalidAccessor(fieldName => $"{fieldName} alanı geçerli değil.");
    messages.SetValueIsInvalidAccessor(value => $"Girilen değer geçerli değil: {value}");
    messages.SetValueMustBeANumberAccessor(fieldName => $"{fieldName} alanı sayı olmalıdır.");
    messages.SetValueMustNotBeNullAccessor(fieldName => $"{fieldName} alanı zorunludur.");
});
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ActivityLogService>();
builder.Services.AddScoped<ChatBotCommandService>();
builder.Services.AddScoped<IProjectImageRepository, ProjectImageRepository>();
builder.Services.AddScoped<IProjectFileStorageService, ProjectFileStorageService>();
builder.Services.AddScoped<IProjectImageService, ProjectImageService>();
builder.Services.AddSingleton<LoginAttemptTracker>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"message\":\"Cok fazla giris denemesi yapildi. Lutfen biraz sonra tekrar deneyin.\"}",
            cancellationToken);
    };

    options.AddPolicy("LoginPolicy", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            clientIp,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

var databaseProvider = builder.Configuration["DatabaseProvider"] ?? "SqlServer";
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (databaseProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
        return;
    }

    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

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
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

var app = builder.Build();

if (app.Environment.IsDevelopment() &&
    databaseProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
    dbContext.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_Email ON Users (Email);");
    dbContext.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS ProjectImages (
            Id INTEGER NOT NULL CONSTRAINT PK_ProjectImages PRIMARY KEY AUTOINCREMENT,
            ProjectId INTEGER NOT NULL,
            FileName TEXT NOT NULL,
            OriginalFileName TEXT NOT NULL,
            ContentType TEXT NOT NULL,
            FilePath TEXT NOT NULL,
            FileSize INTEGER NOT NULL,
            IsCover INTEGER NOT NULL,
            CreatedAt TEXT NOT NULL,
            CONSTRAINT FK_ProjectImages_Projects_ProjectId
                FOREIGN KEY (ProjectId)
                REFERENCES Projects (Id)
                ON DELETE CASCADE
        );
        """);
    dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_ProjectImages_ProjectId ON ProjectImages (ProjectId);");

    if (!dbContext.Roles.Any(r => r.Id == 6))
    {
        dbContext.Roles.Add(new Role
        {
            Id = 6,
            Name = "Emlak",
            Description = "Emlak ve daire satis sureclerini takip eder."
        });
        dbContext.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseSession();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();
