namespace LannisterPay.Models
{
    public class Transaction
    {
        public long Id { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string CurrencyCountry { get; set; }
        public CustomerDetails Customer { get; set; }
        public PaymentEntityDetails PaymentEntity { get; set; }

    }
}
