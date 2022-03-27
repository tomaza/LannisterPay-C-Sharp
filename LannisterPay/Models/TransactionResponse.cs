namespace LannisterPay.Models
{
    public class TransactionResponse
    {
        public string AppliedFeeID { get; set; }
        public decimal AppliedFeeValue { get; set; }
        public decimal ChargeAmount { get; set; }
        public decimal SettlementAmount { get; set; }

    }
}
