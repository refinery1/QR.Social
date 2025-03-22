namespace QR.social.Server.Models
{
    public class QRCodeEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? UserId { get; set; } // Nullable for static QRs
        public string TargetUrl { get; set; } = string.Empty;
        public bool IsDynamic { get; set; } = false;
        public DateTime Created { get; set; } = DateTime.UtcNow;

        public User? User { get; set; } // Navigation property (optional)
    }
}
