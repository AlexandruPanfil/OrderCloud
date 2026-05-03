namespace OrderCloud.Shared.Models
{
    public class VerifyPinByDeviceResponse
    {
        public bool IsValid { get; set; }
        public Guid? LocalUserId { get; set; }
        public string LocalUserName { get; set; } = string.Empty;
    }
}

