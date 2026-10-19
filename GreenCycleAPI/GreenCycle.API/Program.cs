using GreenCycle.Application.Interfaces.Services;
using GreenCycle.Infrastructure.Data;
using GreenCycle.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Hỗ trợ Render tự gán cổng PORT linh hoạt
var renderPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(renderPort))
{
    builder.WebHost.UseUrls($"http://*:{renderPort}");
}

// 1. Cấu hình DB Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<GreenCycleDbContext>(options =>
    options.UseSqlServer(connectionString, x => x.UseNetTopologySuite()));

// 2. Đăng ký Dependency Injection cho Services
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ITransactionNotifier, GreenCycle.API.Services.SignalRTransactionNotifier>();
builder.Services.AddScoped<IVoucherService, VoucherService>();
builder.Services.AddScoped<IScrapYardService, ScrapYardService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserAddressService, UserAddressService>();
builder.Services.AddMemoryCache(); // Dùng để lưu OTP tạm thời
builder.Services.AddScoped<IOtpSender, EmailOtpSender>(); // Gửi mã OTP qua Email (Gmail SMTP / Cloud)
builder.Services.AddHttpClient<IPushNotificationService, FcmPushNotificationService>(c => c.Timeout = TimeSpan.FromSeconds(10)); // Firebase Cloud Messaging Push Notification


// 3. Cấu hình JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };

    // Đọc token từ query string 'access_token' cho kết nối WebSocket SignalR
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, GreenCycle.API.Hubs.UserIdProvider>();
builder.Services.AddEndpointsApiExplorer();

// Thêm cấu hình CORS cho phép Flutter Web (Chrome) và SignalR
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        // SignalR yêu cầu AllowCredentials, không thể kết hợp với AllowAnyOrigin
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// 4. Cấu hình Swagger UI (Chuẩn Swashbuckle)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "GreenCycle API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập token dạng: Bearer {your_token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
    });
});

var app = builder.Build();

// Kích hoạt Swagger UI ngay tại trang chủ để dễ dàng test trên Render
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "GreenCycle API v1");
    c.RoutePrefix = string.Empty; // Truy cập trực tiếp link Render là ra Swagger
});

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Kích hoạt CORS (Phải đặt trước UseAuthentication và UseAuthorization)
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<GreenCycle.API.Hubs.TransactionHub>("/hubs/transaction");

app.Run();