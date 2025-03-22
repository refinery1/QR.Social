using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QR.social.Server.Data;
using QR.social.Server.Models;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=qr.db"));
builder.Services.AddHttpContextAccessor();

// JWT setup (temporary for testing)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "https://qr.social",
            ValidAudience = "https://qr.social",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your-secret-key-here-32-chars-minimum"))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Static QR (public)
app.MapPost("/api/qr/static", (string url) =>
{
    var qrGenerator = new QRCodeGenerator();
    var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
    var qrCode = new QRCode(qrCodeData);
    using var bitmap = qrCode.GetGraphic(20);
    using var stream = new MemoryStream();
    bitmap.Save(stream, ImageFormat.Png);
    return Results.File(stream.ToArray(), "image/png");
})
.WithOpenApi();

// Dynamic QR (authenticated)
app.MapPost("/api/qr/dynamic", [Authorize] async (AppDbContext db, string url, IHttpContextAccessor http) =>
{
    var userId = http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var qr = new QRCodeEntity { UserId = userId, TargetUrl = url };
    db.QRCodes.Add(qr);
    await db.SaveChangesAsync();
    var qrGenerator = new QRCodeGenerator();
    var qrCodeData = qrGenerator.CreateQrCode($"https://qr.social/qr/{qr.Id}", QRCodeGenerator.ECCLevel.Q);
    var qrCode = new QRCode(qrCodeData);
    using var bitmap = qrCode.GetGraphic(20);
    using var stream = new MemoryStream();
    bitmap.Save(stream, ImageFormat.Png);
    return Results.File(stream.ToArray(), "image/png");
})
.WithOpenApi();

// Update Dynamic QR (authenticated)
app.MapPut("/api/qr/dynamic/{id}", [Authorize] async (AppDbContext db, string id, string url, IHttpContextAccessor http) =>
{
    var userId = http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var qr = await db.QRCodes.FirstOrDefaultAsync(q => q.Id == id && q.UserId == userId);
    if (qr == null) return Results.NotFound();
    qr.TargetUrl = url;
    await db.SaveChangesAsync();
    return Results.Ok();
})
.WithOpenApi();

// Redirect for QR scans
app.MapGet("/qr/{id}", async (AppDbContext db, string id) =>
{
    var qr = await db.QRCodes.FindAsync(id);
    return qr != null ? Results.Redirect(qr.TargetUrl) : Results.NotFound();
})
.WithOpenApi();

app.Run();