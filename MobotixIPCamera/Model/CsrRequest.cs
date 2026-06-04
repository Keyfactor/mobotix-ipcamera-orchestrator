// Copyright 2026 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Keyfactor.Extensions.Orchestrator.MobotixIPCamera.Helpers;

/* Model for CSR generation input */
namespace Keyfactor.Extensions.Orchestrator.MobotixIPCamera.Model
{
    public enum KeyAlgorithm
    {
        RSA,
        ECC
    }
    /// <summary>
    /// Input model describing the CSR to be generated.
    /// </summary>
    public record CsrRequest
    {
        public string Subject { get; }
        public KeyAlgorithm Algorithm { get; }
        public int? RsaKeySize { get; }
        public ECCurve? EccCurve { get; }
        public IDictionary<string, string[]>? SANs { get; }
        
        // Main constructor
        public CsrRequest(
            string subject,
            KeyAlgorithm keyAlgorithm = Model.KeyAlgorithm.RSA,
            int? rsaKeySize = 2048,
            ECCurve? eccCurve = null,
            IDictionary<string, string[]>? sans = null)
        {
            Subject = subject;
            Algorithm = keyAlgorithm;
            RsaKeySize = rsaKeySize;
            EccCurve = eccCurve;
            SANs = SANBuilder.NormalizeSANs(sans);
        }
        
        // Alternative constructor using string input for key algorithm and key size
        public CsrRequest(
            string subject,
            string algorithm,
            string? keyParameter = null,
            IDictionary<string, string[]>? sans = null)
        {
            Subject = subject;
            Algorithm = algorithm.ToUpperInvariant() switch
            {
                "RSA" => KeyAlgorithm.RSA,
                "ECC" or "ECP" or "ECDSA" => KeyAlgorithm.ECC,
                _ => throw new ArgumentException("Invalid key algorithm", nameof(algorithm))
            };

            if (Algorithm == KeyAlgorithm.RSA)
            {
                RsaKeySize = ParseRsaKeySize(keyParameter);
                EccCurve = null;
            }
            else if (Algorithm == KeyAlgorithm.ECC)
            {
                RsaKeySize = null;
                EccCurve = ParseEccCurve(keyParameter);

            }
            
            SANs = SANBuilder.NormalizeSANs(sans);
        }
        
        private static int ParseRsaKeySize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 2048;

            if (!int.TryParse(value, out var parsed))
                throw new ArgumentException("Invalid RSA key size", nameof(value));

            if (parsed != 2048 && parsed != 3072 && parsed != 4096)
                throw new ArgumentException("RSA key size must be 2048, 3072, or 4096");

            return parsed;
        }
        
        private static ECCurve? ParseEccCurve(string? value)
        {
            return value switch
            {
                null => ECCurve.NamedCurves.nistP256,
                "256" => ECCurve.NamedCurves.nistP256,
                "384" => ECCurve.NamedCurves.nistP384,
                "521" => ECCurve.NamedCurves.nistP521,
                _ => throw new ArgumentException("Invalid ECC curve")
            };
        }
    }
}