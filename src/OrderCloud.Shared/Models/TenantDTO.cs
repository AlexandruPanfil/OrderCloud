using System.Security.Cryptography;

namespace OrderCloud.Shared.Models
{
    public class TenantDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;
        public string? ApplicationUserId { get; set; }
    }    
}

