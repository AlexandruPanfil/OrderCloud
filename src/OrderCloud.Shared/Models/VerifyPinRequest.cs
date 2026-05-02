namespace OrderCloud.Shared.Models
{
    public class VerifyPinRequest
    {
        public Guid LocalUserId { get; set; }
        public string PinCode { get; set; } = string.Empty;
    }
}