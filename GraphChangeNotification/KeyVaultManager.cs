// <copyright file="KeyVaultManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace TeamsGraphChangeNotification
{
    using System;
    using System.Threading.Tasks;
    using Azure.Identity;
    using Azure.Security.KeyVault.Certificates;
    using Azure.Security.KeyVault.Secrets;
    using Microsoft.Extensions.Options;
    using Models;

    public class KeyVaultManager
    {
        private string EncryptionCertificate;
        private string DecryptionCertificate;
        private string EncryptionCertificateId;
        private readonly IOptions<KeyVaultOptions> KeyVaultOptions;

        public KeyVaultManager(IOptions<KeyVaultOptions> keyVaultOptions)
        {
            KeyVaultOptions = keyVaultOptions;
        }

        public async Task<string> GetEncryptionCertificate()
        {
            // Always renewing the certificate when creating or renewing the subscription so that the certificate
            // can be rotated/changed in key vault without having to restart the application
            await this.GetCertificateFromKeyVault().ConfigureAwait(false);
            return this.EncryptionCertificate;
        }

        public async Task<string> GetDecryptionCertificate()
        {
            if (string.IsNullOrEmpty(DecryptionCertificate))
            {
                await this.GetCertificateFromKeyVault().ConfigureAwait(false);
            }

            return DecryptionCertificate;
        }

        public async Task<string> GetEncryptionCertificateId()
        {
            if (string.IsNullOrEmpty(this.EncryptionCertificateId))
            {
                await this.GetCertificateFromKeyVault().ConfigureAwait(false);
            }

            return this.EncryptionCertificateId;
        }

        private async Task GetCertificateFromKeyVault()
        {
            try
            {
                string clientId = KeyVaultOptions.Value.ClientId;
                string clientSecret = KeyVaultOptions.Value.ClientSecret;
                string tenantId = KeyVaultOptions.Value.TenantId;
                Uri keyVaultUrl = KeyVaultOptions.Value.KeyVaultUrl;
                string keyName = KeyVaultOptions.Value.KeyName;

                ClientSecretCredential credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                SecretClient secretClient = new SecretClient(keyVaultUrl, credential);
                CertificateClient certificateClient = new CertificateClient(keyVaultUrl, credential);


                KeyVaultSecret keyVaultCertificatePfx = await secretClient.GetSecretAsync(keyName).ConfigureAwait(false);
                KeyVaultCertificate keyVaultCertificateCer = await certificateClient.GetCertificateAsync(keyName).ConfigureAwait(false);

                DecryptionCertificate = keyVaultCertificatePfx.Value;
                this.EncryptionCertificate = Convert.ToBase64String(keyVaultCertificateCer.Cer);
                this.EncryptionCertificateId = keyVaultCertificatePfx.Properties.Version;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
