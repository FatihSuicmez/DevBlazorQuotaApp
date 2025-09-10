// Program.cs - GÜNCELLENMİŞ HALİ

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuotaApp.Presentation.Components; // App.razor için yeni adres
using QuotaApp.Presentation.Components.Account; // Identity componentleri için yeni adres
using QuotaApp.Infrastructure; // ApplicationDbContext için yeni adres
using QuotaApp.Domain.Entities; // ApplicationUser için yeni adres
using QuotaApp.Presentation.ApiEndpoints; // API endpointleri için yeni adres
using QuotaApp.Application; // IQuotaService için yeni adres
using QuotaApp.Application.Services; // QuotaService için yeni adres
using QuotaApp.Application.Settings; // QuotaSettings için yeni adres

var builder = WebApplication.CreateBuilder(args);


var quotaSettings = new QuotaSettings();
builder.Configuration.GetSection(QuotaSettings.SectionName).Bind(quotaSettings);
builder.Services.AddSingleton(quotaSettings);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddEndpointsApiExplorer(); // Minimal API'leri keşfetmek için gereklidir.
builder.Services.AddSwaggerGen(); // // Swagger JSON üreticisini ekler.

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// DİKKAT: DbContext ve User'ın yeni namespace'leri kullanılıyor
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.Services.AddHttpClient();

// DİKKAT: IQuotaService kaydını yeni namespace'lere göre güncelledik
builder.Services.AddScoped<IQuotaService, QuotaService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

// DİKKAT: App component'inin yeni adresi kullanılıyor
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// DİKKAT: API endpoint'lerimizin yeni adresi kullanılıyor
app.MapSearchApiEndpoints();

app.Run();