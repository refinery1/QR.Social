namespace QR.social.Server.Models
{
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Email { get; set; } = string.Empty;
        public bool SubscriptionActive { get; set; } = false;
        public DateTime Created { get; set; } = DateTime.UtcNow;
    }
}
