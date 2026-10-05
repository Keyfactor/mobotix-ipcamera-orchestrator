// Copyright 2026 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

#nullable enable
using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace Keyfactor.Extensions.Orchestrator.MobotixIPCamera.Helpers
{
    public static class DeviceCertValidator
    {
        private const string SanExtensionOid = "2.5.29.17";

        /// <summary>
        /// This method is a custom HTTP validator that performs the following logic:
        /// 1. Captures the server's TLS certificate for callers that need it (e.g. Inventory's
        ///    fallback when the primary content-based certificate retrieval finds nothing).
        /// 2. If the only validation problem is a hostname/SAN mismatch - the certificate
        ///    otherwise chains to a CA the orchestrator server trusts - and factoryIpAddress is
        ///    provided, checks the certificate's SAN entries for that value instead. This covers
        ///    the camera's factory certificate, whose SAN reflects its IP address at manufacture
        ///    time rather than wherever it is actually deployed today. Chain validation is never
        ///    relaxed by this check - the certificate still has to chain to a trusted CA.
        /// 3. Otherwise, checks the cert against standard .NET SSL policy errors, recording
        ///    specific, human-readable error messages in errorContext so they can be surfaced to
        ///    the end user (via DeviceCertValidationException) rather than a generic pass/fail.
        /// </summary>
        public static Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> GetValidator(
            string? factoryIpAddress, CertificateErrorContext errorContext, ILogger logger,
            Action<X509Certificate2> onCertCaptured)
        {
            return (message, cert, chain, sslPolicyErrors) =>
            {
                if (null == cert)
                {
                    errorContext.Add("SSL Cert is null");
                    return false;
                }

                if (null == chain)
                {
                    errorContext.Add("Server Cert Chain is null");
                    return false;
                }

                onCertCaptured?.Invoke(new X509Certificate2(cert));

                logger.LogTrace("Verifying cert against default system validation...");

                bool certNotAvailable = sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable);
                bool nameMismatch = sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch);
                bool chainErrors = sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors);

                if (nameMismatch && !chainErrors && !certNotAvailable && !string.IsNullOrWhiteSpace(factoryIpAddress))
                {
                    if (MatchesFactoryIpAddress(cert, factoryIpAddress!, logger))
                    {
                        logger.LogDebug($"Certificate SAN matches the recorded factory IP address '{factoryIpAddress}' - treating the hostname mismatch as expected for the camera's factory certificate.");
                        nameMismatch = false;
                    }
                }

                bool sslErrors = false;
                if (certNotAvailable)
                {
                    sslErrors = true;
                    errorContext.Add("The server did not provide a certificate.");
                }
                if (nameMismatch)
                {
                    sslErrors = true;
                    errorContext.Add("The device hostname does not match the CN or SAN in the server's TLS certificate.");
                }
                if (chainErrors)
                {
                    sslErrors = true;
                    chain.Build(cert);

                    foreach (var status in chain.ChainStatus)
                    {
                        errorContext.Add($"Chain status: {status.Status} - {status.StatusInformation}");
                    }
                }

                if (sslErrors)
                {
                    errorContext.Insert(0, "TLS Cert validation failed!!");
                    return false;
                }

                logger.LogDebug("Certificate chain and subject validated!!");
                return true;
            };
        }

        private static bool MatchesFactoryIpAddress(X509Certificate2 cert, string factoryIpAddress, ILogger logger)
        {
            var sanExtension = cert.Extensions[SanExtensionOid];
            if (sanExtension == null)
            {
                logger.LogTrace("Certificate has no Subject Alternative Name extension to check against the recorded factory IP address.");
                return false;
            }

            string sanText = sanExtension.Format(false);
            logger.LogTrace($"Certificate SAN: {sanText}");

            // Match against each individual SAN entry's value exactly, rather than searching for
            // factoryIpAddress as a substring of the whole formatted SAN text. A substring search
            // would let a short recorded value (e.g. a single digit or punctuation character)
            // accidentally match part of an unrelated SAN entry.
            foreach (var entry in sanText.Split(','))
            {
                var trimmedEntry = entry.Trim();
                var equalsIndex = trimmedEntry.IndexOf('=');
                var value = equalsIndex >= 0 ? trimmedEntry[(equalsIndex + 1)..].Trim() : trimmedEntry;

                if (string.Equals(value, factoryIpAddress, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
