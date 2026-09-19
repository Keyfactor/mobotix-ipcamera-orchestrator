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
        /// <summary>
        /// This method is a custom HTTP validator that performs the following logic:
        /// 1. Captures the server's TLS certificate for callers that need it (e.g. Inventory's
        ///    fallback when the primary content-based certificate retrieval finds nothing).
        /// 2. Checks the cert against standard .NET SSL policy errors, recording specific,
        ///    human-readable error messages in errorContext so they can be surfaced to the
        ///    end user (via DeviceCertValidationException) rather than a generic pass/fail.
        /// </summary>
        public static Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> GetValidator(
            CertificateErrorContext errorContext, ILogger logger,
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

                // Check for standard SSL errors
                logger.LogTrace("Verifying cert against default system validation...");
                bool sslErrors = false;
                if (sslPolicyErrors == SslPolicyErrors.None)
                {
                    logger.LogTrace("Certificate chain is valid.");
                }
                else if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable))
                {
                    sslErrors = true;
                    errorContext.Add("The server did not provide a certificate.");
                }
                else if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch))
                {
                    sslErrors = true;
                    errorContext.Add("The device hostname does not match the CN or SAN in the server's TLS certificate.");
                }
                else if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors))
                {
                    sslErrors = true;
                    errorContext.Add("Certificate chain is NOT valid.");

                    if (!chain.Build(cert))
                    {
                        sslErrors = true;
                        errorContext.Add("Could not build the cert chain!");
                    }

                    foreach (var status in chain.ChainStatus)
                    {
                        sslErrors = true;
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
    }
}