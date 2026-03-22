namespace OrderCloud.Blazor.Models
{
    public class Item
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public decimal Total { get; set; }
        public string TVA { get; set; }
        //Link to Order
        public Guid OrderId { get; set; }
        public Order Order { get; set; }
    }
}
