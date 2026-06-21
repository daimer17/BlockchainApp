using BlockchainApp.Models;
using System.Text.Json;

namespace BlockchainApp.Service
{
    public class ColdWalletService
    {
        public void GenerateOfflineTransaction(
            string from,
            string to,
            decimal amount,
            decimal fee,
            string privateKey,
            string filePath)
        {
            var tx = new Transaction(
                from,
                to,
                amount);

            tx.Fee = fee;

            tx.Signature = "SIGNED";

            string json =
                JsonSerializer.Serialize(
                    tx,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

            File.WriteAllText(filePath, json);

            Console.WriteLine(
                $"Offline transaction saved: {filePath}");
        }
    }
}