namespace OrderCloud.Shared.Models
{
    public class CustomerDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string IDNO { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public TenantDTO? Tenant { get; set; }
    }
}

