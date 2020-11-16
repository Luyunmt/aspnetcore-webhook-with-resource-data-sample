// <copyright file="KeyVaultOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace TeamsGraphChangeNotification.Models
{
    public class KeyVaultOptions
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string TenantId { get; set; }
        public Uri KeyVaultUrl { get; set; }
        public string KeyName { get; set; }
    }
}
