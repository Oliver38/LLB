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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
string dbconnection = @"Server=localhost,1433;Database=llb;;User Id=sa;Password=Password123;MultipleActiveResultSets=true;Initial Catalog=llb; Integrated Security=False  ;  TrustServerCertificate=True";
builder.Services.AddDbContextPool<AppDbContext>(options => options.UseSqlServer(dbconnection));
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
IronPdf.License.LicenseKey = "IRONSUITE.SINOAGRIFAM.GMAIL.COM.9026-6239581857-BDIB5FRTBP6JBA-53SBZ3XGQGTL-HAKT552MCCHZ-L2STJR6ULPP5-HZJTWRJLHUVU-M5P6IAY5B2DT-ECXIEW-TXVHV3K437KQEA-DEPLOYMENT.TRIAL-76W6KL.TRIAL.EXPIRES.13.DEC.2025";
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



await app.InitialiseRoles();
//await app.InitialiseUsers();


app.InitialiseDatabase();
//ApplicationBuilderExtension.I
await ApplicationBuilderExtension.InitialiseRoles(app);
//await ApplicationBuilderExtension.InitialiseUsers(app);
ApplicationBuilderExtension.InitialiseDatabase(app);