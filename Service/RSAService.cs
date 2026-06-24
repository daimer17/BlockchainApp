using System.Security.Cryptography;
using System.Text;

namespace BlockchainApp.Service
{
    public class RSAService
    {
        public string SignData(string data, string privateKey)
        {
            using RSA rsa = RSA.Create();

            rsa.FromXmlString(privateKey);

            byte[] dataBytes =
                Encoding.UTF8.GetBytes(data);

            byte[] signature =
                rsa.SignData(
                    dataBytes,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

            return Convert.ToBase64String(signature);
        }

        public bool VerifySignature(
            string data,
            string signature,
            string publicKey)
        {
            using RSA rsa = RSA.Create();

            rsa.FromXmlString(publicKey);

            byte[] dataBytes =
                Encoding.UTF8.GetBytes(data);

            byte[] signatureBytes =
                Convert.FromBase64String(signature);

            return rsa.VerifyData(
                dataBytes,
                signatureBytes,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
        }
    }
}