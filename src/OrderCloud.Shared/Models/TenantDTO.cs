using System.Security.Cryptography;

namespace OrderCloud.Blazor.Models
{
    public class TenantDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
    }    
}
