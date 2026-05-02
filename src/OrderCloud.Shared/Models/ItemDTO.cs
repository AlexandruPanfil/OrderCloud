namespace OrderCloud.Shared.Models
{
    public class ItemDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty; // Инициализация
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public decimal Total { get; set; }
        public string TVA { get; set; } = string.Empty;  // Инициализация
        //Link to Order
        public Guid OrderId { get; set; }
        public OrderDTO? Order { get; set; }
    }
}

