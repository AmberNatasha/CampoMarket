using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.Cookies;
using CampoMarket.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient<ApiClient>(client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"]
        ?? throw new InvalidOperationException("Falta Api:BaseUrl.");

    client.BaseAddress = new Uri(baseUrl);
});
builder.Services.AddHttpClient<ApiRequestClient>(client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"] ?? throw new InvalidOperationException("Falta Api:BaseUrl.");
    client.BaseAddress = new Uri(baseUrl);
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ApiAccountService>();
builder.Services.AddScoped<IUserService>(sp => sp.GetRequiredService<ApiAccountService>());
builder.Services.AddScoped<IPasswordResetService>(sp => sp.GetRequiredService<ApiAccountService>());
builder.Services.AddScoped<ICatalogService, ApiCatalogService>();
builder.Services.AddScoped<ApiCommerceService>();
builder.Services.AddScoped<ICartService>(sp => sp.GetRequiredService<ApiCommerceService>());
builder.Services.AddScoped<IAddressService>(sp => sp.GetRequiredService<ApiAccountService>());
builder.Services.AddScoped<IOrderService>(sp => sp.GetRequiredService<ApiCommerceService>());
builder.Services.AddScoped<IReportService>(sp => sp.GetRequiredService<ApiCommerceService>());
builder.Services.AddScoped<IAuditService>(sp => sp.GetRequiredService<ApiAccountService>());
builder.Services.AddScoped<IProductImageService, ProductImageService>();
builder.Services.AddScoped<IAuthSessionService, AuthSessionService>();
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.AddScoped<IPasswordResetEmailSender, SmtpPasswordResetEmailSender>();
builder.Services.AddScoped<IContactEmailSender, SmtpContactEmailSender>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("CampoMarketLocal", policy =>
        policy.WithOrigins("https://localhost:5001", "http://localhost:5088")
            .AllowAnyHeader()
            .AllowAnyMethod());
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/acceso-denegado";
        options.Cookie.Name = "CampoMarket.Auth.v3";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = false;
    });
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(
        Path.Combine(builder.Environment.ContentRootPath, ".aspnet-data-protection-keys")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseStatusCodePagesWithReExecute("/Error/{0}");

app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.TryAdd("Content-Security-Policy",
        "default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com https://cdn.jsdelivr.net; script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; connect-src 'self'; frame-ancestors 'none';");
    await next();
});

app.UseRouting();

app.UseCors("CampoMarketLocal");
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
   .WithStaticAssets();

app.Run();
