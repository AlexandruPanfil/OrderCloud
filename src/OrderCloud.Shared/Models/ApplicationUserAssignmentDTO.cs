using System;
using System.Collections.Generic;

namespace OrderCloud.Blazor.Models
{
    public class ApplicationUserAssignmentDTO
    {
        public string Id { get; set; } = null!;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<Guid> TenantIds { get; set; } = new();
    }
}
