namespace OrderCloud.Shared.Models
{
    public class CatalogItemDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string TVA { get; set; } = string.Empty;

        // Link to Tenant
        public Guid TenantId { get; set; }
        public TenantDTO? Tenant { get; set; }
    }
}
