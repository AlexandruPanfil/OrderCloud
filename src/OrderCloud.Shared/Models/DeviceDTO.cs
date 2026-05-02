namespace OrderCloud.Shared.Models
{
    public class DeviceDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ActiveTill { get; set; } = DateTime.UtcNow.AddMonths(1);

        // Link to Tenant
        public Guid TenantId { get; set; }
        public TenantDTO? Tenant { get; set; }

        ////Link to Local User
        //public Guid? LocalUserId { get; set; }
        //public LocalUserDTO? LocalUser { get; set; }
    }
}

