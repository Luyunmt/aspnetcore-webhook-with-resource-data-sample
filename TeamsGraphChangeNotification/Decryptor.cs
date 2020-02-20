﻿// <copyright file="Decryptor.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace TeamsGraphChangeNotification
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;

    public class Decryptor
    {
        private static readonly Lazy<int> AESInitializationVectorSize = new Lazy<int>(() =>
        {
            using var provider = new AesCryptoServiceProvider();
            return provider.LegalBlockSizes[0].MinSize;
        });

        public string Decrypt(string encryptedPayload, string encryptedSymmetricKey, string signature, string serializedCertificate)
        {
            if (string.IsNullOrEmpty(encryptedPayload))
            {
                throw new ArgumentNullException("Encrypted payload cannot be null or empty");
            }
            if (string.IsNullOrEmpty(encryptedSymmetricKey))
            {
                throw new ArgumentNullException("Encrypted symmetric key cannot be null or empty");
            }
            if (string.IsNullOrEmpty(signature))
            {
                throw new ArgumentNullException("Signature cannot be null or empty");
            }
            if (string.IsNullOrEmpty(serializedCertificate))
            {
                throw new ArgumentNullException("Certificate cannot be null or empty");
            }
            using var certificate = new X509Certificate2(Convert.FromBase64String(serializedCertificate));
            using var rsaPrivateKey = RSACertificateExtensions.GetRSAPrivateKey(certificate);
            return DecryptPayload(encryptedPayload, encryptedSymmetricKey, signature, rsaPrivateKey);
        }

        private string DecryptPayload(string encryptedData, string encryptedSymmetricKey, string signature, RSA asymmetricPrivateKey)
        {
            try
            {
                byte[] key = asymmetricPrivateKey.Decrypt(Convert.FromBase64String(encryptedSymmetricKey), RSAEncryptionPadding.OaepSHA1);
                using var hashAlg = new HMACSHA256(key);
                string base64String = Convert.ToBase64String(hashAlg.ComputeHash(Convert.FromBase64String(encryptedData)));
                if (!string.Equals(signature, base64String))
                {
                    throw new InvalidDataException("Signature does not match");
                }
                return Encoding.UTF8.GetString(AESDecrypt(Convert.FromBase64String(encryptedData), key));
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Unexpected error occured while trying to decrypt the input", ex);
            }
        }

        private byte[] AESDecrypt(byte[] dataToDecrypt, byte[] key)
        {
            if (dataToDecrypt == null)
            {
                throw new ArgumentNullException("Data to decrypt cannot be null");
            }
            if (key != null)
            {
                if (key.Length >= AESInitializationVectorSize.Value / 8)
                {
                    try
                    {
                        using var cryptoServiceProvider = new AesCryptoServiceProvider
                        {
                            Mode = CipherMode.CBC,
                            Padding = PaddingMode.PKCS7,
                            Key = key
                        };
                        byte[] numArray = new byte[AESInitializationVectorSize.Value / 8];
                        Array.Copy(key, numArray, numArray.Length);
                        cryptoServiceProvider.IV = numArray;
                        using var memoryStream = new MemoryStream();
                        using var cryptoStream = new CryptoStream(memoryStream, cryptoServiceProvider.CreateDecryptor(), CryptoStreamMode.Write);
                        cryptoStream.Write(dataToDecrypt, 0, dataToDecrypt.Length);
                        cryptoStream.FlushFinalBlock();
                        return memoryStream.ToArray();
                    }
                    catch (Exception ex)
                    {
                        throw new ApplicationException("Unexpected error occured while trying to decrypt the input", ex);
                    }
                }
            }
            throw new ArgumentException("Invalid symmetric key:the key size must me at least: " + (AESInitializationVectorSize.Value / 8).ToString());
        }
    }
}
