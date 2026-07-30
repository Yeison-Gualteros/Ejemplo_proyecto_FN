using AutoMapper;
using Contracts;
using Entities.Models;
using FileNova.Exceptions;
using FileNova.Middleware;
using FileNova.Presentation.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using NLog;
using Repository;
using Service;
using Service.Contracts;
using Shared;
using System.Text;

NewtonsoftJsonPatchInputFormatter GetJsonPatchInputFormatter() =>
    new ServiceCollection()
        .AddLogging()
        .AddMvc()
        .AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.ReferenceLoopHandling =
                Newtonsoft.Json.ReferenceLoopHandling.Ignore;
        })
        .Services.BuildServiceProvider()
        .GetRequiredService<IOptions<MvcOptions>>()
        .Value.InputFormatters
        .OfType<NewtonsoftJsonPatchInputFormatter>()
        .First();

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

// ==================================
// Configuración de NLog
// ==================================
var nlogConfigFile = Path.Combine(Directory.GetCurrentDirectory(), "nlog.config");
LogManager.Setup().LoadConfigurationFromFile(nlogConfigFile, optional: false);

// ==================================
// Registro de servicios
// ==================================
builder.Services.ConfigureCors();
builder.Services.ConfigureIISIntegration();
builder.Services.ConfigureLoggerService();
builder.Services.ConfigureRepositoryManager();
builder.Services.ConfigureServiceManager();
builder.Services.ConfigureSqlContext(builder.Configuration);

builder.Services.AddScoped<ValidationFilterAttribute>();
builder.Services.AddScoped<PatchValidationFilterAttribute>();

builder.Services.ConfigueResponseCaching();
builder.Services.ConfigureHttpCacheHeaders();

builder.Services.ConfigureIdentity();
builder.Services.ConfigureJWT(builder.Configuration);
builder.Services.AddJwtConfiguration(builder.Configuration);
builder.Services.ConfigureAuthorizationHandlers();


// EMAIL CONFIGURATION
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings")
);
builder.Services.AddScoped<IEmailService, EmailService>();

// AutoMapper
//builder.Services.AddAutoMapper(typeof(FileNova.MappingProfile).Assembly);
builder.Services.AddSingleton<AutoMapper.IMapper>(sp =>
{
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

    var config = new AutoMapper.MapperConfiguration(
        cfg =>
        {
            cfg.AddProfile<FileNova.MappingProfile>();
        },
        loggerFactory
    );

    return config.CreateMapper();
});

builder.Services.AddDbContext<RepositoryContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("sqlConnection"));

    // ✅ Agregar esta línea para habilitar el interceptor
    options.AddInterceptors(new DbQueryInterceptor());
});

// Desactivar validación automática
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// ==================================
// Configuración de controladores
// ==================================
builder.Services.AddControllers(config =>
{
    config.RespectBrowserAcceptHeader = true;
    config.ReturnHttpNotAcceptable = true;
    config.InputFormatters.Insert(0, GetJsonPatchInputFormatter());
})
.AddXmlDataContractSerializerFormatters()
.AddCustomCSVFormatter()
.AddApplicationPart(typeof(FileNova.Presentation.AssemblyReference).Assembly)
.AddNewtonsoftJson();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\FileNovaKeys"))
    .SetApplicationName("FileNova");
// ==================================
// Construcción del pipeline
// ==================================
var app = builder.Build();

app.UseMiddleware<RequestLoggingMiddleware>();

// Middleware de excepciones global
var logger = app.Services.GetRequiredService<ILoggerManager>();
app.ConfigureExceptionHandler(logger);

// Seguridad en producción
if (app.Environment.IsProduction())
    app.UseHsts();

app.UseHttpsRedirection();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "archivos")),
    RequestPath = "/archivos"
});


app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.All
});


app.UseAuthentication();
app.UseRouting();
app.UseCors("CorsPolicy");
app.UseAuthorization();

app.MapControllers();
app.Run();
