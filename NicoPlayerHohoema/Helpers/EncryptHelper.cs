using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace NicoPlayerHohoema.Helpers
{
    // this code copy from
    // http://www.c-sharpcorner.com/article/encrypt-and-decrypt-string-in-windows-runtime-apps/
    public static class EncryptHelper
    {
        public static string EncryptStringHelper(string plainString, string key)
        {
            var hashKey = GetMD5Hash(key);
            var decryptBuffer = CryptographicBuffer.ConvertStringToBinary(plainString, BinaryStringEncoding.Utf8);
            var AES = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesEcbPkcs7);
            var symmetricKey = AES.CreateSymmetricKey(hashKey);
            var encryptedBuffer = CryptographicEngine.Encrypt(symmetricKey, decryptBuffer, null);
            var encryptedString = CryptographicBuffer.EncodeToBase64String(encryptedBuffer);
            return encryptedString;
        }

        public static string DecryptStringHelper(string encryptedString, string key)
        {
            var hashKey = GetMD5Hash(key);
            IBuffer decryptBuffer = CryptographicBuffer.DecodeFromBase64String(encryptedString);
            var AES = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesEcbPkcs7);
            var symmetricKey = AES.CreateSymmetricKey(hashKey);
            var decryptedBuffer = CryptographicEngine.Decrypt(symmetricKey, decryptBuffer, null);
            string decryptedString = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, decryptedBuffer);
            return decryptedString;
        }

        private static IBuffer GetMD5Hash(string key)
        {
            IBuffer bufferUTF8Msg = CryptographicBuffer.ConvertStringToBinary(key, BinaryStringEncoding.Utf8);
            HashAlgorithmProvider hashAlgorithmProvider = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5);
            IBuffer hashBuffer = hashAlgorithmProvider.HashData(bufferUTF8Msg);
            if (hashBuffer.Length != hashAlgorithmProvider.HashLength)
            {
                throw new Exception("There was an error creating the hash");
            }
            return hashBuffer;
        }
    }
}
