// Copyright 2026 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

#nullable enable
using Keyfactor.Extensions.Orchestrator.MobotixIPCamera.Model;
using Keyfactor.Logging;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

/* CsrService.cs
 * ---------------------------------------------------------------------------------------------------
 * Description:
 * This class serves as the main engine for handling private key creation and CSR generation for reenrollment jobs.
 *
 * Notes:
 * This integration manages only the TLS certificate.
 *
 */
namespace Keyfactor.Extensions.Orchestrator.MobotixIPCamera.Helpers
{
    public class CsrService
    {
        private readonly ILogger _logger;
        
        // In-memory key store
        private readonly ConcurrentDictionary<string, AsymmetricAlgorithm> _keystore = new();

        // Inject logger
        public CsrService(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Generates private key and CSR, stores key internally.
        /// Supports RSA and ECC.
        /// </summary>
        public CsrResult GenerateCsr(CsrRequest request)
        {
            _logger.MethodEntry();

            if (string.IsNullOrWhiteSpace(request.Subject))
            {
                _logger.LogError("Subject is null or empty");
                throw new ArgumentException("Subject cannot be null or empty", nameof(request.Subject));
            }

            AsymmetricAlgorithm key;
            CertificateRequest certRequest;
            
            // Step 1: Create private key
            switch (request.Algorithm)
            {
                case KeyAlgorithm.RSA:
                {
                    var rsa = RSA.Create(request.RsaKeySize ?? 2048); // Defaults to 2048 if no size provided
                    key = rsa;
                    
                    _logger.LogDebug("Created RSA key with size {Size}", request.RsaKeySize ?? 2048);
                    
                    // Step 2: Create CSR --- Add RSA key algorithm and size
                    certRequest = new CertificateRequest(request.Subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                    break;
                }
                case KeyAlgorithm.ECC:
                {
                     var ecdsa = ECDsa.Create(request.EccCurve ?? ECCurve.NamedCurves.nistP256); // Defaults to 256 if no size provided
                     key = ecdsa;
                     
                     _logger.LogDebug("Created ECC key with curve {Curve}", request.EccCurve ?? ECCurve.NamedCurves.nistP256);
                     
                     // Step 2: Create CSR --- Add ECC key algorithm and curve
                     certRequest = new CertificateRequest(request.Subject, ecdsa, HashAlgorithmName.SHA256);

                     break;
                }
                default:
                    _logger.LogError("Unsupported key algorithm");
                    throw new NotSupportedException("Unsupported key algorithm");
            }
            
            // Step 3: Create CSR --- Add Subject Alternative Names (important for TLS)
            if (request.SANs?.Count > 0)
            {
                _logger.LogTrace("Adding Subject Alternative Names to CSR");
                
                var sanBuilder = new SubjectAlternativeNameBuilder();

                foreach (var entry in request.SANs)
                {
                    var type = entry.Key.ToUpperInvariant();
                    var values = entry.Value;

                    foreach (var value in values)
                    {
                        switch (type)
                        {
                            case "DNS":
                                sanBuilder.AddDnsName(value);
                                break;
                            case "IP":
                                if (!System.Net.IPAddress.TryParse(value, out var ip))
                                {
                                    _logger.LogError("Invalid IP address: {Value}", value);
                                    throw new ArgumentException($"Invalid IP address: {value}");
                                }
                                sanBuilder.AddIpAddress(ip);
                                break;
                            case "URI":
                                sanBuilder.AddUri(new Uri(value));
                                break;
                            case "EMAIL":
                            case "RFC822":
                                sanBuilder.AddEmailAddress(value);
                                break;
                            default:
                                _logger.LogWarning($"Unsupported SAN type skipped: {type}");
                                throw new ArgumentException($"Unsupported SAN type: {type}");
                        }
                    }
                }

                certRequest.CertificateExtensions.Add(sanBuilder.Build());
            }


            // Step 4: Create CSR --- Add typical TLS extensions
            _logger.LogDebug("Adding typical TLS extensions to CSR");
            certRequest.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                    critical: false));

            certRequest.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(false, false, 0, false));

            // Step 5: Generate CSR (DER format)
            _logger.LogTrace("Generating CSR");
            byte[] csrDer = certRequest.CreateSigningRequest();

            // Step 6: Convert to PEM
            string csrPem = new string(PemEncoding.Write("CERTIFICATE REQUEST", csrDer));

            // Step 7: Store private key internally (never exposed)
            string keyId = Guid.NewGuid().ToString();
            _keystore[keyId] = key;
            
            _logger.LogInformation($"CSR generated with KeyId: {keyId}");
            _logger.MethodExit();

            return new CsrResult(keyId, csrPem);
        }

        public PemBundle ExportPemBundle(string keyId, X509Certificate2 cert)
        {
            _logger.MethodEntry();

            if (!_keystore.TryGetValue(keyId, out var key))
            {
                _logger.LogError($"Key not found in keystore for KeyId {keyId}");
                throw new InvalidOperationException("Key not found");
            }

            try
            {
                _logger.LogTrace($"Validating certificate vs key match for KeyId {keyId}...");

                // Extract public key from certificate (works for both RSA and ECC)
                byte[] certPublic;
                byte[] keyPublic;
                
                // Extract cert public key
                if (cert.GetRSAPublicKey() is RSA certRsa)
                {
                    certPublic = certRsa.ExportSubjectPublicKeyInfo();
                }
                else if (cert.GetECDsaPublicKey() is ECDsa certEcdsa)
                {
                    certPublic = certEcdsa.ExportSubjectPublicKeyInfo();
                }
                else
                {
                    _logger.LogError("Unsupported certificate key type");
                    throw new NotSupportedException("Unsupported certificate key type");
                }
                
                // Extract private key -> public key
                keyPublic = key switch
                {
                    RSA rsa => rsa.ExportSubjectPublicKeyInfo(),
                    ECDsa ecdsa => ecdsa.ExportSubjectPublicKeyInfo(),
                    _ => throw new NotSupportedException("Unsupported key type")
                };

                bool match = certPublic.SequenceEqual(keyPublic);

                // Logging for debugging purposes
                _logger.LogDebug($"Cert Algorithm: {cert.PublicKey.Oid.FriendlyName}");
                _logger.LogDebug($"Key Type: {key.GetType().Name}");

                _logger.LogDebug("---- Public Key Comparison ----");
                var certPublicBase64 = Convert.ToBase64String(certPublic);
                var keyPublicBase64 = Convert.ToBase64String(keyPublic);
                
                // Log truncated keys
                _logger.LogDebug("Cert Public Key (first 50): {CertKey}",
                    certPublicBase64.Length > 50 ? certPublicBase64[..50] + "..." : certPublicBase64);
                _logger.LogDebug("Key Public Key (first 50): {KeyKey}",
                    keyPublicBase64.Length > 50 ? keyPublicBase64[..50] + "..." : keyPublicBase64);
                
                _logger.LogDebug($"Keys Match: {match}");
                
                if (!match)
                {
                    _logger.LogError($"Public key mismatch detected for KeyId: {keyId}");
                }
                
                // Step 1: Attach private key to certificate
                // Validate key matches certificate (strong validation)
                _ = key switch
                {
                    RSA rsa => cert.CopyWithPrivateKey(rsa),
                    ECDsa ecdsa => cert.CopyWithPrivateKey(ecdsa),
                    _ => throw new NotSupportedException("Unsupported key type"),
                };
                
                _logger.LogTrace("Certificate successfully validated against private key!!");
                
                // Step 2: Export certificate PEM
                _logger.LogTrace("Exporting certificate to PEM format");
                byte[] certRaw = cert.Export(X509ContentType.Cert);
                string certPem = new string(PemEncoding.Write("CERTIFICATE", certRaw));
                    
                // Step 3: Export private key PEM (PKCS#8)
                _logger.LogTrace("Exporting private key to PEM format");
                byte[] keyRaw = ExportPrivateKey(key);
                string keyPem = new string(PemEncoding.Write("PRIVATE KEY", keyRaw));
                
                _logger.LogInformation($"Successfully exported PEM bundle for KeyId: {keyId}");
                _logger.MethodExit();
                
                return new PemBundle(certPem, keyPem); 
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed to export PEM bundle for KeyId: {keyId}");
                throw;

            }
        }
        
        private static byte[] ExportPrivateKey(AsymmetricAlgorithm key)
        {
            return key switch
            {
                RSA rsa => rsa.ExportPkcs8PrivateKey(),
                ECDsa ecdsa => ecdsa.ExportPkcs8PrivateKey(),
                _ => throw new NotSupportedException("Unsupported key type")
            };
        }
        
        public static void ValidateCsr(string csrPem)
        {
            try
            {
                PemReader pemReader = new PemReader(new StringReader(csrPem));
                Pkcs10CertificationRequest csr = (Pkcs10CertificationRequest)pemReader.ReadObject();

                if (!csr.Verify())
                {
                    throw new Exception("CSR signature verification failed.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"CSR Validation failed: {ex.Message}");
            }
        }
    }
}