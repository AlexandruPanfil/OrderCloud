using Microsoft.AspNetCore.Identity;
using OrderCloud.Blazor.Models;

namespace OrderCloud.Blazor.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        //Link to multiple Tenants
        public ICollection<TenantDTO> Tenants { get; set; } = new List<TenantDTO>();
    }

}
