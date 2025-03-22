using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QR.social.Server.Data;
using QR.social.Server.Models;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Claims;

namespace QR.social.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QRCodesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public QRCodesController(AppDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpPost("static")]
    public IActionResult CreateStatic([FromBody] string url)
    {
        var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new QRCoder.QRCode(qrCodeData); // Line 31 - Fully qualified
        using var bitmap = qrCode.GetGraphic(20);
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return File(stream.ToArray(), "image/png");
    }

    [HttpPost("dynamic")]
    [Authorize]
    public async Task<IActionResult> CreateDynamic([FromBody] string url)
    {
        var userId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var qr = new QRCodeEntity { UserId = userId, TargetUrl = url, IsDynamic = true };
        _db.QRCodes.Add(qr);
        await _db.SaveChangesAsync();
        var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode($"https://qr.social/qr/{qr.Id}", QRCodeGenerator.ECCLevel.Q);
        var qrCode = new QRCoder.QRCode(qrCodeData); // Line 48 - Fully qualified
        using var bitmap = qrCode.GetGraphic(20);
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return File(stream.ToArray(), "image/png");
    }

    [HttpPut("dynamic/{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateDynamic(string id, [FromBody] string url)
    {
        var userId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var qr = await _db.QRCodes.FirstOrDefaultAsync(q => q.Id == id && q.UserId == userId && q.IsDynamic);
        if (qr == null) return NotFound();
        qr.TargetUrl = url;
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("/qr/{id}")]
    public async Task<IActionResult> RedirectQR(string id)
    {
        var qr = await _db.QRCodes.FindAsync(id);
        return qr != null ? Redirect(qr.TargetUrl) : NotFound();
    }
}