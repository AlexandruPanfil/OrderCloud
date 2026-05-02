using Microsoft.AspNetCore.Identity;
using OrderCloud.Shared.Models;

namespace OrderCloud.Shared.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        //Link to multiple Tenants
        public ICollection<TenantDTO> Tenants { get; set; } = new List<TenantDTO>();
    }

}

