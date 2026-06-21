using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockchainApp.Models;

namespace BlockchainApp.Service
{
    public static class TransactionService
    {
        private static readonly CryptoService cryptoService = new CryptoService();

        public static Transaction CreateTransaction(string from, string to, decimal amount, string privateKey)
        {
            var tx = new Transaction(from, to, amount);
            SignTransaction(tx, privateKey);
            var validation = ValidateTransaction(tx);
            if (!validation.isValid)
            {
                throw new ValidationException(validation.error);
            }

            return tx;
        }

        public static (bool isValid, string error) ValidateTransaction(Transaction transaction)
        {
            if (transaction == null) return (false, "Transaction is null.");
            if (string.IsNullOrEmpty(transaction.From)) return (false, "Sender required.");
            if (string.IsNullOrEmpty(transaction.To)) return (false, "Recipient required.");
            if (transaction.Amount <= 0) return (false, "Amount must be > 0");

            return (true, string.Empty);
        }

        public static void SignTransaction(Transaction transaction, string privateKey)
        {
            var signature = cryptoService.SignData(
                transaction.ToRawString(),
                privateKey
            );

            transaction.Signature = signature;
        }
    }
}
