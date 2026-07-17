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

builder.Services.AddHttpClient();

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

       IronPdf.License.LicenseKey = "IRONSUITE.NEWDAWNSOLAR3.GMAIL.COM.23297-7A33D32845-AIWX5EJ-WAIESQQ5GC64-44RDJ3754SQO-XQF7ZDX7FQNJ-D4NSOC6C57W7-V6WLRJ5EVP4B-VSX7UX7D4ZP6-XLI46J-T4WGPKEGASCREA-DEPLOYMENT.TRIAL-VUWO67.TRIAL.EXPIRES.04.JUL.2026";
       //"IRONSUITE.UENERGYZIM.GMAIL.COM.18341-B96BE9DD54-DSH4G-IMLVFZT7LUVR-HZ3R4XN32X3V-5GVVKEBITGZG-DZB4FMBJ42KK-HIXSYLRTHBWK-AXWPFVVKV4LK-BJRWEW-TJDUVBDV7MCREA-DEPLOYMENT.TRIAL-UJFMPO.TRIAL.EXPIRES.10.JUN.2026";
//IronPdf.License.LicenseKey = "IRONSUITE.CHIMUKAOLIVER.GMAIL.COM.12332-DB80BC55B8-AGWBH5YLDYE3UIWH-XRXOF7T7CXIV-W6MVY6EUSDXV-DCTN5IBDAPJ4-SGYMBWOJ2DON-JFYJUW6SCCUR-EQGFQJ-TJPJM47EDAOREA-DEPLOYMENT.TRIAL-IQRDZ7.TRIAL.EXPIRES.24.APR.2026";
//IronPdf.License.LicenseKey = "IRONSUITE.OCHIMUKA.TTCSGLOBAL.COM.23477-13EACBA4B3-C4SBO-DNXFDTCCZ5HR-ECICSHFMCEW2-3IEBKW4RNC6C-XDJQF7U3GW2E-EIYS2IZ6E5RH-WVORH5SKJ73E-VR5PDE-TIUAQYASQUCNEA-DEPLOYMENT.TRIAL-INU7AW.TRIAL.EXPIRES.31.AUG.2024";

//IronPdf.License.LicenseKey = "IRONPDF-ZIVDERO-MY-TRIAL-LICENSE-KEY-EXPIRES.15.MAR.2022";
//IronPdf.License.LicenseKey = "IRONSUITE.OCHIMUKA.LSU.AC.ZW.4007-AF20386C1E-B3SGOFLSAXCACFEW-Y6IFKIR4G4JI-7UAZUCTH6T5L-RRFYTZSJAUXK-HINXG2L7PZZX-IPGFBKO3LIJ2-U3BWRP-TGLS3K3FUESNEA-DEPLOYMENT.TRIAL-4FDGKI.TRIAL.EXPIRES.31.JUL.2024";
// "IRONPDF-17941296BC-181444-E7D0E8-C74E6C9B01-A2555F9F-UExAAF2885A4F327D8-DELPLOYMENT.TRIAL.EXPIRES.18.AUG.2019";

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
