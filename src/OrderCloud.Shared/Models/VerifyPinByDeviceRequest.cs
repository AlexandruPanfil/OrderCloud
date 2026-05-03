namespace OrderCloud.Shared.Models
{
    public class VerifyPinByDeviceRequest
    {
        public Guid DeviceId { get; set; }
        public string PinCode { get; set; } = string.Empty;
    }
}

