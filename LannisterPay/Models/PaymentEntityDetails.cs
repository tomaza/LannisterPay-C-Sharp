namespace LannisterPay.Models
{
    public class PaymentEntityDetails
    {
        public long Id { get; set; }
        public string Issuer { get; set; } = string.Empty;
        public string Brand { get; set; }
        public string Number { get; set; }
        public string SixID { get; set; }
        public string Type { get; set; }
        public string Country { get; set; }
    }
}
