using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using OfficeOpenXml;
using PROHUB.Data;
using PROHUB.Services;
using ProHub.Data;
using ProHub.Data.Interfaces;
using ProHub.Data.Repositories;
using System.Security.Claims;
using ProHub.Constants;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// ==============================
// FORWARDED HEADERS (FOR REVERSE PROXY)
// ==============================
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Clear known networks and proxies to trust the incoming headers from Nginx
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// ==============================
// AUTHENTICATION
// ==============================
builder.Services.AddAuthentication(options =>
{
    // Cookie = default scheme
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    // Azure AD OpenID Connect = challenge scheme
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;

})
.AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddMicrosoftGraph(builder.Configuration.GetSection("DownstreamApi"))
    .AddInMemoryTokenCaches();



// ===============================
// SESSION (REQUIRED FOR LOGIN LOGIC)
// ===============================
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1); // session lifetime
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


// Add Razor Views + Azure Identity UI
builder.Services.AddControllersWithViews();
//.AddMicrosoftIdentityUI();

// ===============================
// OPENID CONNECT EVENTS (FIXED)
// ===============================
// ===============================
// OPENID CONNECT EVENTS (FULL FIXED)
// ===============================
builder.Services.Configure<OpenIdConnectOptions>(
    OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
        options.TokenValidationParameters.NameClaimType = ClaimTypes.Email;

        // Redirect after login
        options.Events.OnRedirectToIdentityProvider = context =>
        {
            context.Properties.RedirectUri = "/Home/Index";
            return Task.CompletedTask;
        };

       options.Events.OnTokenValidated = async context =>
{
    var email = context.Principal?.FindFirst(ClaimTypes.Email)?.Value
             ?? context.Principal?.FindFirst("preferred_username")?.Value
             ?? context.Principal?.FindFirst("upn")?.Value;

    if (string.IsNullOrEmpty(email))
    {
        context.Response.Redirect("/Account/AuthError?reason=email_not_found");
        context.HandleResponse();
        return;
    }

    var repo = context.HttpContext.RequestServices.GetRequiredService<IEmployeePermissionRepository>();
    var employee = repo.GetEmployeeByEmail(email);

    if (employee == null)
    {
        context.Response.Redirect("/Account/AuthError?reason=user_not_in_employee_table");
        context.HandleResponse();
        return;
    }

   

    // -------------------------
    // USERS ROLE ASSIGNMENT
    // -------------------------
    string appRole = ProHub.Constants.AppRoles.ViewOnly; // default

    var groupName = employee.Group?.GroupName ?? string.Empty;


    // -------------------------
    // BLOCK INACTIVE USERS
    // -------------------------
    if (groupName.Contains("Inactive", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Redirect("/Account/AuthError?reason=inactive");
        context.HandleResponse(); // stops login
        return;
    }

    // -------------------------
    // ACTIVE USERS ASSIGNMENT
    // -------------------------
     else if (groupName.Contains("Administrator", StringComparison.OrdinalIgnoreCase))
         appRole = ProHub.Constants.AppRoles.Admin;
     else if (groupName.Contains("Non Developer", StringComparison.OrdinalIgnoreCase))
         appRole = ProHub.Constants.AppRoles.NonDeveloper;
     else if (groupName.Contains("Developer", StringComparison.OrdinalIgnoreCase))
         appRole = ProHub.Constants.AppRoles.Developer;
     else if (groupName.Contains("DPO User", StringComparison.OrdinalIgnoreCase))
         appRole = ProHub.Constants.AppRoles.DPO;
     else if (groupName.Contains("Ishamp User", StringComparison.OrdinalIgnoreCase))
         appRole = ProHub.Constants.AppRoles.Ishamp;

    // -------------------------
    // ADD CLAIMS
    // -------------------------
    var customClaims = new List<Claim>
    {
        new Claim("EmployeeId", employee.EmpId.ToString()),
        new Claim(ClaimTypes.Role, appRole),
        new Claim("app_role", appRole)
    };

    var identity = new ClaimsIdentity(customClaims);
    context.Principal.AddIdentity(identity);
};

        // Handle remote errors
        options.Events.OnRemoteFailure = context =>
        {
            // Get the actual error message
            var errorMessage = context.Failure?.Message ?? "unknown";

            // Optional: encode for URL
            errorMessage = Uri.EscapeDataString(errorMessage);

            // Redirect to your error page with actual reason
            context.Response.Redirect($"/Account/AuthError?reason={errorMessage}");

            context.HandleResponse(); // stop further processing
            return Task.CompletedTask;
        };

    });



// ===============================
// REPOSITORIES
// ===============================
builder.Services.AddScoped<ExternalSolutionRepository>();
builder.Services.AddScoped<ConsumerPlatformRepository>();
builder.Services.AddScoped<EmployeeRepository>();
builder.Services.AddScoped<NextWeekPlanRepository>();
builder.Services.AddScoped<DocumentRepository>();
builder.Services.AddScoped<PartnerRepository>();
builder.Services.AddScoped<OvertimeRepository>();
builder.Services.AddScoped<IFreelancerService, FreelancerService>();
builder.Services.AddScoped<IInternalSolutionService, InternalSolutionDataAccess>();
builder.Services.AddScoped<IExternalSolutionService, ExternalSolutionDataAccess>();
builder.Services.AddScoped<IExternalIssueService, ExternalIssueDataAccess>();
builder.Services.AddScoped<IInternalIssueService, InternalIssueDataAccess>();
builder.Services.AddScoped<IInternalSolutionsActivitiesService, InternalSolutionsActivitiesDataAccess>();
builder.Services.AddScoped<IExternalSolutionsActivitiesService, ExternalSolutionsActivitiesDataAccess>();
builder.Services.AddScoped<ITraineeService, TraineeDataAccess>();
builder.Services.AddScoped<IEmployeePermissionRepository, EmployeePermissionRepository>();
builder.Services.AddScoped<IInternalSolutionInprogressService, InternalSolutionInprogressDataAccess>();
builder.Services.AddScoped<IExternalSolutionsProspectiveService, ExternalSolutionsProspectiveDataAccess>();
builder.Services.AddScoped<ProHub.Data.CompanyRepository>();
builder.Services.AddScoped<IRecentlyLaunchedService, RecentlyLaunchedDataAccess>();



builder.Services.ConfigureApplicationCookie(options =>
{
    // Redirect forbidden requests to your AccountController.AccessDenied action
    options.AccessDeniedPath = "/Account/AccessDenied";
});



// Excel License
ExcelPackage.License.SetNonCommercialPersonal("ProHub Application");

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();


app.UseSession();

// ✅ Detect expired session
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        // Session expired
        if (!context.Session.Keys.Contains("LoginLogged"))
        {
            context.Items["SessionExpired"] = true;
        }
    }

    await next();
});

app.UseRouting();


app.UseAuthentication();
app.UseAuthorization();

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Index}/{id?}");

app.Run();
