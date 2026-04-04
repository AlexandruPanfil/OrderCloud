using System.Text.Json.Serialization;

namespace OrderCloud.Blazor.Models
{
    public class BillDTO
    {
        public Guid Id { get; set; }
        public DateTime BillDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty; // Cash or Card
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
        public string ReceiptContent { get; set; } = string.Empty;
        
        // Link to Tenant
        public Guid? TenantId { get; set; }
        public TenantDTO? Tenant { get; set; }
        
        // Bill Items
        public List<BillItemDTO> Items { get; set; } = new();
    }
    
    public class BillItemDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public string TVA { get; set; } = string.Empty;
        public decimal Total { get; set; }
        
        // Link to Bill
        public Guid BillId { get; set; }
        
        [JsonIgnore]
        public BillDTO? Bill { get; set; }
    }
}
