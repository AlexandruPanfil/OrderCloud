namespace OrderCloud.Blazor.Models
{
    public class OrderDTO
    {
        public Guid Id { get; set; }
        public string Status { get; set; }
        public decimal Total { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        //Link to Tenant
        public Guid TenantId { get; set; }
        public TenantDTO Tenant { get; set; }
        //Link to Local User
        public Guid? LocalUserId { get; set; }
        public LocalUserDTO? LocalUser { get; set; }
        //Link to Customer
        public Guid CustomerId { get; set; }
        public CustomerDTO Customer { get; set; }
        //Link to Items
        public List<ItemDTO> Items { get; set; }
    }
}
