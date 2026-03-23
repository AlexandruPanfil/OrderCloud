namespace OrderCloud.Blazor.Models
{
    public class LocalUserDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string PinCode { get; set; }
        //Link to Device
        public Guid ?DeviceId { get; set; }
        public DeviceDTO ?Device { get; set; }
        //Link to Tenant
        public Guid TenantId { get; set; }
        public TenantDTO Tenant { get; set; }
    }
}
