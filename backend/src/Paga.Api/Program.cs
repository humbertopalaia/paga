using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Paga.Api.Configuration;
using Paga.Api.ExceptionHandling;
using Paga.Api.Services;
using Paga.Application.Abstractions;
using Paga.Application.Validators;
using Paga.Infrastructure;
using Paga.Infrastructure.Persistence;
using Paga.Infrastructure.Security;
using Paga.Infrastructure.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services));

    // Fail-fast configuration validation
    builder.Configuration.ValidateConnectionString();
    builder.Configuration.ValidateJwtKey();

    // CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                          ?? ["http://localhost:4200"];
            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // Infrastructure (DbContext, password hasher, seeder)
    builder.Services.AddInfrastructure(builder.Configuration);

    // Authentication (JWT Bearer)
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            var key = builder.Configuration["Jwt:Key"]!;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                ClockSkew = TimeSpan.Zero
            };
        });

    builder.Services.AddAuthorization();

    // Controllers
    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<Paga.Api.Filters.FluentValidationFilter>();
    });

    // FluentValidation auto-validation with assembly scanning
    builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

    // Exception handling
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // HttpContextAccessor (required by CurrentUserService)
    builder.Services.AddHttpContextAccessor();

    // Application services
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IExpenseTypeService, ExpenseTypeService>();
    builder.Services.AddScoped<IIncomeService, IncomeService>();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    // Health checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<PagaDbContext>();

    // Swagger/OpenAPI
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    });

    var app = builder.Build();

    // Middleware pipeline
    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();

    app.UseCors("AllowFrontend");

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { status = report.Status.ToString() });
        }
    });

    // Database seeding
    using (var scope = app.Services.CreateScope())
    {
        var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
        await seeder.SeedAsync();
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Entry point marker for WebApplicationFactory in integration tests.
/// </summary>
public partial class Program;
