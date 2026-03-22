using System.Security.Cryptography;
using OrderCloud.Blazor.Data;

namespace OrderCloud.Blazor.Models
{
    public class Tenant
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        //Link to AplicationUser
        public string? ApplicationUserId { get; set; }
        public ApplicationUser? ApplicationUser { get; set; }
    }    
}
