using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlockchainApp.Service
{
    public class CryptoService
    {
        public (string publicKey, string privateKey) GenerateKeyPair()
        {
            using (var rsa = RSA.Create())
            {
                var publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
                var privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
                return (publicKey, privateKey);
            }
        }

        public string SignData(string data, string privateKey)
        {
            using (var rsa = RSA.Create())
            {
                rsa.ImportRSAPrivateKey(
                    Convert.FromBase64String(privateKey),
                    out _
                );

                var dataBytes = Encoding.UTF8.GetBytes(data);

                var signatureBytes = rsa.SignData(
                    dataBytes,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1
                );

                return Convert.ToBase64String(signatureBytes);
            }
        }
        public bool VerifySignature(string data, string signature, string publicKey)
        {
            using (var rsa = RSA.Create())
            {
                rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _);
                var dataBytes = Encoding.UTF8.GetBytes(data);


                var signatureBytes = Convert.FromBase64String(signature);

                return rsa.VerifyData(
                    dataBytes,
                    signatureBytes,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1
                );
            }
        }
    }
}
