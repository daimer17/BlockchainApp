using System.Text;
using System.Threading.Tasks;

namespace BlockchainApp.Models
{
    public class Transaction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TransactionId { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public decimal Fee { get; set; }
        public string TokenSymbol { get; set; } = "MAIN";
        public string Signature { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
        public int LockTime { get; set; } = 0;

        public Transaction(string from,  string to, decimal amount)
        {
            From = from;
            To = to;
            Amount = amount;
            TransactionId = Guid.NewGuid().ToString();
        }

        public string ToRawString()
        {
            return $"{From}{To}{Amount}{Fee}{TokenSymbol}{TimeStamp}";
        }

        public override string ToString()
        {
            return $"Transaction: {Id}, From: {From}, To: {To}, Amount: {Amount}, TimeStamp: {TimeStamp}";
        }
    }
}