using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using OrderCloud.Shared.Data;

namespace OrderCloud.Shared.Models
{
    public class TenantDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;
        public string? ApplicationUserId { get; set; }

        [NotMapped]
        public List<string> ApplicationUserIds { get; set; } = new();

        [JsonIgnore]
        public ICollection<ApplicationUser> ApplicationUsers { get; set; } = new List<ApplicationUser>();
    }    
}

