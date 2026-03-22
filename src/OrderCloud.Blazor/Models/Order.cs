namespace OrderCloud.Blazor.Models
{
    public class Order
    {
        public Guid Id { get; set; }
        public string Status { get; set; }
        public decimal Total { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        //Link to Tenant
        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }
        //Link to Local User
        public Guid? LocalUserId { get; set; }
        public LocalUser? LocalUser { get; set; }
        //Link to Items
        public List<Item> Items { get; set; }
    }
}
