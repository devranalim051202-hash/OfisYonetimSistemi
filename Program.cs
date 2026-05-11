using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OfisYonetimSistemi.Models;
using OfisYonetimSistemi.Services;
using System.Text;

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
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();
