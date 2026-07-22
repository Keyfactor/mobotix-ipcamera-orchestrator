// Copyright 2026 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Keyfactor.Extensions.Orchestrator.MobotixIPCamera.Model
{
    /* Model for HTTP Result from /config/camera/media --- Used to capture results from Download Endpoint */
    public class CameraMediaCertResult
    {
        public bool HasCertificate { get; set; }
        public IEnumerable<string> CertChain { get; set; }
        public MediaResponse Metadata { get; set; }
    }

    public class MediaResponse
    {
        public Dictionary<string, CertificateInfo> X509Key { get; set; }
        public Dictionary<string, object> image { get; set; }
        public Dictionary<string, object> sound { get; set; }

    }
    
    public class CertificateInfo
    {
        public long mtime { get; set; }
        public string name { get; set; }
        public string sha1 { get; set; }
        public int size { get; set; }
        public string time { get; set; }
    }

    public class HTTPResponseParser
    {
        public static CameraMediaCertResult ParseDownloadResponse(byte[] rawBytes, string baseRestClientUrl = "")
        {
            var raw = Encoding.UTF8.GetString(rawBytes);
            var certResult = new CameraMediaCertResult();
            
            // Detect if certificate is present
            certResult.HasCertificate = raw.Contains("-----BEGIN CERTIFICATE-----");
            
            // Extract JSON metadata
            var jsonStart = raw.IndexOf('{');
            var jsonEnd = raw.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = raw.Substring(jsonStart, jsonEnd - jsonStart + 1);
                certResult.Metadata = JsonConvert.DeserializeObject<MediaResponse>(json);
            }
            
            // Extract cert chain, if present
            if (certResult.HasCertificate)
            {
                // Use cert returned from API ---
                // Extract each certificate in the chain into separate strings
                var matches = Regex.Matches(
                    raw,
                    "-----BEGIN CERTIFICATE-----.*?-----END CERTIFICATE-----",
                    RegexOptions.Singleline
                );
                
                certResult.CertChain = matches.Select(m => m.Value).ToList();
            }
            else
            {
                // Fallback to TLS
                /*var tlsCert = GetTlsCertificate(baseRestClientUrl);

                if (tlsCert != null)
                {
                    certResult.CertChain = new List<string> { ExportToPem(tlsCert) };
                }

                certResult.HasCertificate = true;*/
            }
            
            return certResult;
        }

        /*public static X509Certificate2 GetTlsCertificate(string url)
        {
            X509Certificate2 capturedCert = null;

            var handler = new HttpClientHandler();

            handler.ServerCertificateCustomValidationCallback = (request, cert, chain, errors) =>
            {
                // Capture the certificate from TLS handshake
                if (cert != null)
                {
                    capturedCert = new X509Certificate2(cert);
                }

                // Always return true to allow the connection to proceed 
                // Allow self-signed / invalid certs
                return true;
            };
                
            using var client = new HttpClient(handler);

            try
            {
                // Trigger TLS handshake
                client.GetAsync(url).Wait();
            }
            catch 
            {
                // Even if request fails, cert may still be captured
            }
            
            return capturedCert;
        }*/

    }
}
