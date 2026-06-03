// Copyright 2026 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

using Newtonsoft.Json;

using Keyfactor.Logging;
using Keyfactor.Orchestrators.Extensions;
using Keyfactor.Extensions.Orchestrator.MobotixIPCamera.Model;
using Keyfactor.Extensions.Orchestrator.MobotixIPCamera.Helpers;
using Keyfactor.Extensions.Orchestrator.MobotixIPCamera.Client;
using Keyfactor.Orchestrators.Common.Enums;

namespace Keyfactor.Extensions.Orchestrator.MobotixIPCamera
{
    public class Reenrollment: IReenrollmentJobExtension
    {
        private readonly ILogger<Reenrollment> _logger;
        private readonly CsrService _csrService;
        
        public string ExtensionName => "";
        
        public Reenrollment()
        {
            // Register services
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddNLog();
            });
            
            services.AddTransient<CsrService>();
            
            // Build provider
            var provider = services.BuildServiceProvider();
            
            // Resolve dependencies
            _logger = provider.GetRequiredService<ILogger<Reenrollment>>();
            _csrService = provider.GetRequiredService<CsrService>();
        }
        
        // Job Entry Point
        public JobResult ProcessJob(ReenrollmentJobConfiguration config, SubmitReenrollmentCSR submitReenrollment)
        {
            _logger.MethodEntry();
            
            try
            {
                _logger.LogTrace($"Beginning Reenrollment for Client Machine {config.CertificateStoreDetails.ClientMachine}...");
                string jsonConfig = JsonConvert.SerializeObject(config);
                _logger.LogDebug($"Reenrollment Config: {jsonConfig.Replace(config.ServerPassword,"**********")}");
                
                // Log each key-value pair in the Job Properties for debugging
                _logger.LogDebug("Begin Job Properties ---");
                foreach (var itm in config.JobProperties)
                {
                    _logger.LogDebug($"{itm.Key}:{itm.Value}");
                }
                _logger.LogDebug("--- End Job Properties");
                
                // Log each SAN, if provided
                _logger.LogDebug("Begin SANs ---");
                var formattedSANs = SANBuilder.BuildSANList(config.SANs,_logger);
                if (formattedSANs.Count == 0)
                {
                    _logger.LogDebug($"No SAN values found.");
                }
                else
                {
                    foreach (var san in formattedSANs)
                    {
                        _logger.LogDebug($"{san}");
                    }   
                }
                _logger.LogDebug("--- End SANs");
                
                // Get required reenrollment fields
                string keyAlgorithm = config.JobProperties["keyType"].ToString() ?? throw new Exception("Key Algorithm returned null");
                string keySize = config.JobProperties["keySize"].ToString() ?? throw new Exception("Key Size returned null");
                string subject = config.JobProperties["subjectText"].ToString() ?? throw new Exception("Subject returned null");
                string newAlias = config.Alias ?? throw new Exception("Alias returned null");
                _logger.LogDebug($"Alias: {newAlias}");
                
                _logger.LogTrace("Create private key pair and generate CSR");
                var result = _csrService.GenerateCsr(
                    new CsrRequest(
                        subject,
                        keyAlgorithm,
                        keySize,
                        config.SANs
                    )
                );
                _logger.LogDebug($"CSR: \n{result.CsrPem}");
                
                _logger.LogTrace("Validating CSR");
                CsrService.ValidateCsr(result.CsrPem);
                _logger.LogTrace("CSR is valid");
                
                // Submit CSR to be signed
                _logger.LogTrace("Submitting CSR to Command to enroll for signed certificate");
                var x509Cert = submitReenrollment.Invoke(result.CsrPem);
                
                _logger.LogTrace($"Exporting PEM bundle with KeyId: {result.KeyId}");
                if (x509Cert is null)
                {
                    throw new Exception("Certificate returned null");
                }
                var pemBundle = _csrService.ExportPemBundle(result.KeyId,x509Cert);
                
                _logger.LogTrace($"Certificate to Upload: {pemBundle.CertificatePem}");
                
                // TODO: Remove the logging of private key
                _logger.LogTrace($"Key to Upload: {pemBundle.PrivateKeyPem}");
                
                _logger.LogTrace("Create HTTPS client to connect to device");
                var client = new MobotixHttpClient(config, config.CertificateStoreDetails);
                
                // Upload the private key to the device
                _logger.LogTrace("Uploading private key to device");
                client.UploadPemFile(pemBundle.PrivateKeyPem, "httpd_privkey.pem");
                
                // Upload the certificate to the device
                _logger.LogTrace("Uploading certificate to device");
                client.UploadPemFile(pemBundle.CertificatePem, "httpd_cert.pem");
                
                // Reboot the device to apply the certificate and private key
                _logger.LogTrace("Rebooting device to apply changes");
                client.RebootDevice();
                
                _logger.MethodExit();
            }
            catch (Exception ex)
            {
                //Status: 2=Success, 3=Warning, 4=Error
                return new JobResult() { Result = OrchestratorJobStatusJobResult.Failure, JobHistoryId = config.JobHistoryId, 
                    FailureMessage = $"Reenrollment Job Failed: {ex.Message} - Refer to logs for more detailed information." };
            }
            
            //Status: 2=Success, 3=Warning, 4=Error
            return new JobResult() { Result = OrchestratorJobStatusJobResult.Success, JobHistoryId = config.JobHistoryId };
        }
    }
}