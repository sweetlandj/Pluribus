﻿// The MIT License (MIT)
// 
// Copyright (c) 2017 Jesse Sweetland
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Platibus.Config;
using Platibus.Config.Extensibility;
#if NETSTANDARD2_0
using Microsoft.Extensions.Configuration;
#endif

namespace Platibus.Security
{
    /// <inheritdoc />
    /// <summary>
    /// Provider for initializing an <see cref="T:Platibus.Security.AesMessageEncryptionService" />
    /// </summary>
    [Provider("AES")]
    public class AesMessageEncryptionServiceProvider : IMessageEncryptionServiceProvider
    {
#if NET452
        /// <inheritdoc />
        public Task<IMessageEncryptionService> CreateMessageEncryptionService(EncryptionElement configuration)
        {
            IMessageEncryptionService messageEncryptionService = null;
            var enabled = configuration.Enabled;
            if (enabled)
            {
                var key = (HexEncodedSecurityKey) configuration.Key;
                if (key == null)
                {
                    throw new ConfigurationErrorsException("Attribute 'key' is required for AES message encryption service");
                }

                var fallbackKeys = configuration.FallbackKeys.Select(k => (HexEncodedSecurityKey) k.Key).ToList();
                var aesOptions = new AesMessageEncryptionOptions(key)
                {
                    FallbackKeys = fallbackKeys
                };
                messageEncryptionService = new AesMessageEncryptionService(aesOptions);
            }
            return Task.FromResult(messageEncryptionService);
        }
#endif
#if NETSTANDARD2_0
        /// <inheritdoc />
        public Task<IMessageEncryptionService> CreateMessageEncryptionService(IConfiguration configuration)
        {
            IMessageEncryptionService messageEncryptionService = null;
            var enabled = configuration != null && configuration.GetValue("enabled", false);
            if (enabled)
            {
                var key = (HexEncodedSecurityKey) configuration.GetValue<string>("key");
                if (key == null)
                {
                    throw new ConfigurationErrorsException("Attribute 'key' is required for AES message encryption service");
                }

                var fallbackKeys = configuration.GetSection("fallbackKeys")
                    .GetChildren()
                    .Select(k => (HexEncodedSecurityKey) k.Value)
                    .ToList();

                var aesOptions = new AesMessageEncryptionOptions(key)
                {
                    FallbackKeys = fallbackKeys
                };
                messageEncryptionService = new AesMessageEncryptionService(aesOptions);
            }
            return Task.FromResult(messageEncryptionService);
        }
#endif
    }
}