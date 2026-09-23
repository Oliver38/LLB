using DNTCaptcha.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using LLB.Data;
using LLB.Models;
using LLB.Extensions;
using Microsoft.AspNetCore.Builder;
using LLB.Helpers;
using LLB.Services;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);
PaynowCurrencyHelper.Configure(builder.Configuration.GetSection("PaynowIntegrations"));
PaynowCurrencyHelper.SetCurrentPaymentMode(builder.Configuration["PaymentSettings:CurrentMode"]);

// Add services to the container.
builder.Services.AddControllersWithViews();
var dbConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing database connection string: ConnectionStrings:DefaultConnection");
var dbConnectionBuilder = new SqlConnectionStringBuilder(dbConnection);

builder.Services.AddDbContextPool<AppDbContext>(options => options.UseSqlServer(dbConnectionBuilder.ConnectionString));
builder.Services.AddScoped<TaskAllocationHelper>();
builder.Services.AddIdentity<ApplicationUser, IdentityRole>().AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();

builder.Services.AddMemoryCache();
builder.Services.Configure<CompanyRegistryOptions>(
    builder.Configuration.GetSection(CompanyRegistryOptions.SectionName));
builder.Services.AddHttpClient<ICompanyRegistryClient, CompanyRegistryClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<CompanyRegistryOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds));
});

builder.Services.AddMvc(options =>
{
    var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.Filters.Add(new AuthorizeFilter(policy));
}).AddXmlSerializerFormatters();
builder.Services.AddDNTCaptcha(options =>
{
    options.UseCookieStorageProvider()
               .ShowThousandsSeparators(false);
    options.WithEncryptionKey("MzAyMjc1QDMxMzgyZTMxMmUzMG51ZitKTHRHc2Z4aFY0U3NGelJGRk5jYWxnZzN0QXRJYjZaclZ0dktmdFE9");
}
       );

var ironPdfLicenseKey = builder.Configuration["IronPdf:LicenseKey"];
if (!string.IsNullOrWhiteSpace(ironPdfLicenseKey))
{
    IronPdf.License.LicenseKey = ironPdfLicenseKey;
}

//builder.WebHost.ConfigureKestrel(options =>
//{
//    options.Limits.MaxRequestBodySize = 52428800; // 50MB
//});


builder.Services.Configure<IdentityOptions>(options =>
{
    // Password settings.
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings.
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(2);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings.
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+*?";
    options.User.RequireUniqueEmail = true;
});
builder.Services.ConfigureApplicationCookie(options =>
{
    //// Cookie settings
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(1000);

    options.LoginPath = "/";  // Set the path to the login page
    options.AccessDeniedPath = "/";
    options.SlidingExpiration = true;
});
builder.Services.Configure<PasswordHasherOptions>(options =>
               options.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3
           );
var app = builder.Build();

var commandLine = string.Join(" ", Environment.GetCommandLineArgs()).ToLowerInvariant();
var isEfTooling = commandLine.Contains("dotnet-ef") || commandLine.Contains("ef.dll");
var runDatabaseStartupTasks = builder.Configuration.GetValue("Database:RunStartupTasks", true);

if (!isEfTooling && runDatabaseStartupTasks)
{
    try
    {
        app.InitialiseDatabase();
        await app.InitialiseRoles();
    }
    catch (Exception ex)
    {
        app.Logger.LogCritical(ex, "Database startup tasks failed. Check ConnectionStrings:DefaultConnection and SQL Server availability.");
        throw;
    }
}
else if (!isEfTooling)
{
    app.Logger.LogWarning("Database startup tasks were skipped because Database:RunStartupTasks is false.");
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=LandingPage}/{id?}");

app.Run();
