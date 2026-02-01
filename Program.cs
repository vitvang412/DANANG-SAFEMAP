using DaNangSafeMap.Data;
using DaNangSafeMap.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
});

// Configure MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Configure Authentication
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["GoogleAuthSettings:ClientId"]!;
        options.ClientSecret = builder.Configuration["GoogleAuthSettings:ClientSecret"]!;
        options.CallbackPath = "/signin-google";
        options.SaveTokens = true;

        // Lấy thêm thông tin từ Google
        options.Scope.Add("profile");
        options.Scope.Add("email");

        // Map claims từ Google
        options.ClaimActions.MapJsonKey("urn:google:picture", "picture", "url");
        options.ClaimActions.MapJsonKey("urn:google:locale", "locale", "string");

        options.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            if (context.Properties.Items.ContainsKey("prompt"))
            {
                context.Response.Redirect(context.RedirectUri + "&prompt=" + context.Properties.Items["prompt"]);
            }
            else
            {
                context.Response.Redirect(context.RedirectUri);
            }
            return Task.CompletedTask;
        };
    });

// Register services
builder.Services.AddScoped<IAuthService, AuthService>();
// builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>(); // Removed as logic is in Controller
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    // Only use SameSite=None and Secure in production
    if (builder.Environment.IsProduction())
    {
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    }
    else
    {
        options.Cookie.SameSite = SameSiteMode.Lax;
    }
});

var app = builder.Build();

// Configure pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCookiePolicy();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();