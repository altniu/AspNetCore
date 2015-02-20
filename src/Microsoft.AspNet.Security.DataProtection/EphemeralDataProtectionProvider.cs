﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Cryptography.Cng;
using Microsoft.AspNet.Security.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNet.Security.DataProtection.Cng;
using Microsoft.AspNet.Security.DataProtection.KeyManagement;

namespace Microsoft.AspNet.Security.DataProtection
{
    /// <summary>
    /// An IDataProtectionProvider that is transient.
    /// </summary>
    /// <remarks>
    /// Payloads generated by a given EphemeralDataProtectionProvider instance can only
    /// be deciphered by that same instance. Once the instance is lost, all ciphertexts
    /// generated by that instance are permanently undecipherable.
    /// </remarks>
    public sealed class EphemeralDataProtectionProvider : IDataProtectionProvider
    {
        private readonly KeyRingBasedDataProtectionProvider _dataProtectionProvider;

        public EphemeralDataProtectionProvider()
        {
            IKeyRingProvider keyringProvider;

            if (OSVersionUtil.IsBCryptOnWin7OrLaterAvailable())
            {
                // Fastest implementation: AES-GCM
                keyringProvider = new EphemeralKeyRing<CngGcmAuthenticatedEncryptorConfigurationOptions>();
            }
            else
            {
                // Slowest implementation: managed CBC + HMAC
                keyringProvider = new EphemeralKeyRing<ManagedAuthenticatedEncryptorConfigurationOptions>();
            }

            _dataProtectionProvider = new KeyRingBasedDataProtectionProvider(keyringProvider);
        }

        public IDataProtector CreateProtector([NotNull] string purpose)
        {
            // just forward to the underlying provider
            return _dataProtectionProvider.CreateProtector(purpose);
        }

        private sealed class EphemeralKeyRing<T> : IKeyRing, IKeyRingProvider
            where T : IInternalConfigurationOptions, new()
        {
            // Currently hardcoded to a 512-bit KDK.
            private const int NUM_BYTES_IN_KDK = 512 / 8;

            public IAuthenticatedEncryptor DefaultAuthenticatedEncryptor { get; } = new T().CreateAuthenticatedEncryptor(Secret.Random(NUM_BYTES_IN_KDK));

            public Guid DefaultKeyId { get; } = default(Guid);

            public IAuthenticatedEncryptor GetAuthenticatedEncryptorByKeyId(Guid keyId, out bool isRevoked)
            {
                isRevoked = false;
                return (keyId == default(Guid)) ? DefaultAuthenticatedEncryptor : null;
            }

            public IKeyRing GetCurrentKeyRing()
            {
                return this;
            }
        }
    }
}
