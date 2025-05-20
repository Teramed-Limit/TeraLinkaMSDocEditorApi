using System.Text;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using TeraLinkaMSDocEditorApi.Application.Services;
using TeraLinkaMSDocEditorApi.Infrastructure.Authentication;
using TeraLinkaMSDocEditorApi.Infrastructure.Mappings;
using TeraLinkaMSDocEditorApi.Infrastructure.Persistence;
using TeraLinkaMSDocEditorApi.Web.Hubs;

var builder = WebApplication.CreateBuilder(args);

// 獲取應用配置
var configuration = builder.Configuration;

// Add services to the container.
builder.Services.AddScoped<DocumentService>();

// 添加SignalR服務
builder.Services.AddSignalR();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "Doc API",
            Version = "v1",
            Description = "Doc API Description"
        }
    );

    // 添加 JWT 認證配置
    c.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Description =
                "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        }
    );

    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        }
    );
});

// Add services to the container.
builder.Services.AddControllers();

// 設定 Serilog 作為日誌處理器
Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();

// 使用 Serilog 作為日誌處理器
builder.Host.UseSerilog();

// 設定靜態文件服務
builder.Services.AddSpaStaticFiles(configuration => { configuration.RootPath = "ClientApp/build"; });

// 配置 CORS 以允許所有來源的請求
builder.Services.AddCors(options =>
{
    var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>();
    options.AddPolicy(
        "AllowAll",
        policy => { policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials(); }
    );
});

// 配置 JWT 認證
var useJwtAuth = configuration.GetValue<bool>("UseJwtAuth", false);

// 添加認證服務
var auth = builder.Services.AddAuthentication(options =>
{
    if (useJwtAuth)
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }
    else
    {
        options.DefaultAuthenticateScheme = "NoAuth";
        options.DefaultChallengeScheme = "NoAuth";
    }
});

if (useJwtAuth)
{
    auth.AddJwtBearer(options =>
    {
        var secret = configuration.GetSection("JWTSecret").Get<string>();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "teramed",
            ValidateAudience = true,
            ValidAudience = "teramed",
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
        };
    });
}
else
{
    builder.Services.AddAuthentication(options =>
    {
        options.AddScheme<AllowAllAuthenticationHandler>("NoAuth", "No Authentication");
    });
}

// 配置 DbContext
var connectionString = configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString).EnableSensitiveDataLogging();
    if (Convert.ToBoolean(configuration["SQLDebug"]))
        options.LogTo(Console.WriteLine);
});

// 設定 AutoMapper
builder.Services.AddSingleton(provider =>
    new MapperConfiguration(cfg =>
    {
        cfg.AddProfile(new AutoMapperProfiles());
        // cfg.AddProfile(new AutoMapperProfiles(provider.GetService<ConfigService>()));
    }).CreateMapper()
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Documents")))
    Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Documents"));

// 允許前端與 OnlyOffice 存取文件
app.UseStaticFiles();

// 配置應用以服務靜態文件和 SPA
app.UseDefaultFiles();
app.UseSpaStaticFiles();

app.UseRouting();

// 使用 CORS
app.UseCors("AllowAll");

// 添加 Serilog 請求記錄
if (app.Environment.IsDevelopment())
    app.UseSerilogRequestLogging();

// 添加認證中間件（無論是否使用 JWT 都需要）
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<DocumentHub>("/documentHub");
});

app.UseSpa(spa => { spa.Options.SourcePath = "ClientApp"; });

app.Run();