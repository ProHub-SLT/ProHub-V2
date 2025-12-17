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
                // Redirect to custom error page with specific reason
                context.Response.Redirect("/Account/AuthError?reason=email_not_found");
                context.HandleResponse(); // Stop the authentication process
                return;
            }

            var repo = context.HttpContext.RequestServices.GetRequiredService<IEmployeePermissionRepository>();
            var employee = repo.GetEmployeeByEmail(email);

            if (employee == null)
            {
                context.Response.Redirect("/Account/AuthError?reason=user_not_in_employee_table");
                context.HandleResponse(); // Stop the authentication process
                return;
            }

            var azureRoles = context.Principal.Claims
                .Where(c => c.Type.EndsWith("/role", StringComparison.OrdinalIgnoreCase)
                         || c.Type.Equals("roles", StringComparison.OrdinalIgnoreCase))
                .Select(c => c.Value)
                .ToList();



            // BLOCK INACTIVE USERS
            if (azureRoles.Contains(AppRoles.Inactive, StringComparer.OrdinalIgnoreCase))
            {
                context.Response.Redirect("/Account/AuthError?reason=inactive");
                context.HandleResponse();
                return;
            }


            string appRole = ProHub.Constants.AppRoles.ViewOnly;

            if (azureRoles.Any(r => r.Contains("Administrator", StringComparison.OrdinalIgnoreCase)))
                appRole = AppRoles.Admin;
            else if (azureRoles.Contains("NonDeveloper", StringComparer.OrdinalIgnoreCase))
                appRole = AppRoles.NonDeveloper;
            else if (azureRoles.Any(r => r.Contains("Developer", StringComparison.OrdinalIgnoreCase)))
                appRole = AppRoles.Developer;
            else if (azureRoles.Contains("DPOUser", StringComparer.OrdinalIgnoreCase))
                appRole = AppRoles.DPO;
            else if (azureRoles.Contains("IshampUser", StringComparer.OrdinalIgnoreCase))
                appRole = AppRoles.Ishamp;
            


            // FIXED: do NOT use ClaimTypes.Role
            var customClaims = new List<Claim>
            {
                new Claim("EmployeeId", employee.EmpId.ToString()),
               

                // 🔑 Used by ASP.NET authorization
                new Claim(ClaimTypes.Role, appRole),

                // 🎨 Used ONLY for profile UI
                new Claim("app_role", appRole)
            };

            var identity = new ClaimsIdentity(customClaims);
            context.Principal.AddIdentity(identity);
        };

        // OnRemoteFailure event for generic errors
        options.Events.OnRemoteFailure = context =>
        {
            context.Response.Redirect("/Account/AuthError?reason=unknown");
            context.HandleResponse();
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
