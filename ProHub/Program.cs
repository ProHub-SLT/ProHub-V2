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

var builder = WebApplication.CreateBuilder(args);

// ===============================
// AUTHENTICATION
// ===============================
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

// Add Razor Views + Azure Identity UI
builder.Services.AddControllersWithViews();
                //.AddMicrosoftIdentityUI();

// ===============================
// OPENID CONNECT EVENTS (FIXED)
// ===============================
builder.Services.Configure<OpenIdConnectOptions>(
    OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role; 
        options.TokenValidationParameters.NameClaimType = ClaimTypes.Email;

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
                context.Fail("Email not found in Azure token.");
                return;
            }

            var repo = context.HttpContext.RequestServices.GetRequiredService<IEmployeePermissionRepository>();
            var employee = repo.GetEmployeeByEmail(email);

            if (employee == null)
            {
                context.Fail("Access Denied: User not in Employee table.");
                return;
            }

            var azureRoles = context.Principal.Claims
                .Where(c => c.Type.EndsWith("/role", StringComparison.OrdinalIgnoreCase)
                         || c.Type.Equals("roles", StringComparison.OrdinalIgnoreCase))
                .Select(c => c.Value)
                .ToList();

            string appRole = ProHub.Constants.AppRoles.ViewOnly;
            if (azureRoles.Any(r => r.Contains("admin", StringComparison.OrdinalIgnoreCase)))
                appRole = ProHub.Constants.AppRoles.Admin;
            else if (azureRoles.Any(r => r.Contains("developer", StringComparison.OrdinalIgnoreCase)))
                appRole = ProHub.Constants.AppRoles.Developer;

            // FIXED: do NOT use ClaimTypes.Role
            var customClaims = new List<Claim>
            {
                new Claim("EmployeeId", employee.EmpId.ToString()),
                new Claim("AppRole", appRole)
            };

            var identity = new ClaimsIdentity(customClaims);
            context.Principal.AddIdentity(identity);
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




builder.Services.ConfigureApplicationCookie(options =>
{
    // Redirect forbidden requests to your AccountController.AccessDenied action
    options.AccessDeniedPath = "/Account/AccessDenied";
});



// Excel License
ExcelPackage.License.SetNonCommercialPersonal("ProHub Application");

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Index}/{id?}");

app.Run();
