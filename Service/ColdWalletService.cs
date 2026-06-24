using BlockchainApp.Models;
using System.Text.Json;
using System.Security.Cryptography;

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

            using RSA rsa = RSA.Create();

            rsa.FromXmlString(privateKey);

            tx.PublicKey =
                rsa.ToXmlString(false);

            var rsaService =
                new RSAService();

            tx.Signature =
                rsaService.SignData(
                    tx.ToRawString(),
                    privateKey);

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