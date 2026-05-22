// Copyright 2026 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Keyfactor.Extensions.Orchestrator.MobotixIPCamera.Model
{
    /* Model for Client Certificates represented in Keyfactor Command */
    public class Certificate
    {
        public string Alias { get; set; }
        public IEnumerable<string> CertChainAsPem { get; set; }
        
        public override string ToString()
        {
            return CertChainAsPem == null ? string.Empty : string.Join(Environment.NewLine + Environment.NewLine, CertChainAsPem);
        }
        public static string ExportToPem(X509Certificate2 cert)
        {
            var builder = new StringBuilder();

            builder.AppendLine("-----BEGIN CERTIFICATE-----");
            builder.AppendLine(Convert.ToBase64String(cert.Export(X509ContentType.Cert),
                Base64FormattingOptions.InsertLineBreaks));
            builder.AppendLine("-----END CERTIFICATE-----");

            return builder.ToString();
        }
    }

    public class CertificateData
    {
        public List<Certificate> Certs { get; set; } = new List<Certificate>();
    }

}
