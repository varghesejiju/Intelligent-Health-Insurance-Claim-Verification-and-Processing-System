using HealthInsuranceClaims.Application.Interfaces;
using HealthInsuranceClaims.Application.Services;
using HealthInsuranceClaims.Domain.Entities;
using HealthInsuranceClaims.Infrastructure.Data;
using HealthInsuranceClaims.Infrastructure.ML;
using HealthInsuranceClaims.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add Database Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.MigrationsAssembly("HealthInsuranceClaims.Infrastructure");
        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        sqlOptions.CommandTimeout(60);
    }));

builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

// Add ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.LogoutPath = "/Account/Logout";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// Register Application and Infrastructure Services
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IFileStorageService, PhysicalFileStorageService>();
builder.Services.AddScoped<IPolicyService, PolicyService>();
builder.Services.AddScoped<IHospitalService, HospitalService>();
builder.Services.AddScoped<IClaimVerificationService, ClaimVerificationService>();
builder.Services.AddScoped<IClaimWorkflowService, ClaimWorkflowService>();
builder.Services.AddScoped<IClaimService, ClaimService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IClaimAiService, ClaimAiService>();

// Add MVC Services
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Ensure Secure Uploads Directory exists
var uploadDirectory = builder.Configuration.GetValue<string>("FileStorage:UploadDirectory") ?? "App_Data/SecureUploads";
var absoluteUploadDir = Path.IsPathRooted(uploadDirectory) 
    ? uploadDirectory 
    : Path.Combine(app.Environment.ContentRootPath, uploadDirectory);
if (!Directory.Exists(absoluteUploadDir))
{
    Directory.CreateDirectory(absoluteUploadDir);
}

// Seed Database automatically on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await DbInitializer.SeedAsync(context, userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
