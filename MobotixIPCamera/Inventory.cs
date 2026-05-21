// Copyright 2026 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;

using Keyfactor.Extensions.Orchestrator.MobotixIPCamera.Client;
using Microsoft.Extensions.Logging;

using Keyfactor.Logging;
using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Orchestrators.Extensions;
using Newtonsoft.Json;

using Keyfactor.Extensions.Orchestrator.MobotixIPCamera.Model;

namespace Keyfactor.Extensions.Orchestrator.MobotixIPCamera
{
    public class Inventory : IInventoryJobExtension
    {
        private readonly ILogger _logger;
        
        public string ExtensionName => "";
        
        public Inventory()
        {
            _logger = LogHandler.GetClassLogger<Inventory>();
        }
        
        // Job Entry Point
        public JobResult ProcessJob(InventoryJobConfiguration config, SubmitInventoryUpdate submitInventory)
        {
            List<CurrentInventoryItem> inventoryItems = new List<CurrentInventoryItem>();
            var warningFlag = false;

            try
            {
                _logger.MethodEntry();
                
                _logger.LogInformation($"Beginning Inventory for Client Machine {config.CertificateStoreDetails.ClientMachine}...");
                string jsonConfig = JsonConvert.SerializeObject(config, Formatting.Indented);
                _logger.LogTrace($"Inventory Config: {jsonConfig.Replace(config.ServerPassword,"**********")}");
                
                _logger.LogTrace("Create HTTPS client to connect to device");
                var client = new MobotixHttpClient(config, config.CertificateStoreDetails);
                
                // Perform client cert inventory
                _logger.LogTrace("Retrieve TLS certificate");
                CertificateData data = client.ListCertificates();
                
                // Build the list of client certificates and add to the InventoryItems object sent back to Command
                inventoryItems.AddRange(data.Certs.Select(
                    c =>
                    {
                        try
                        {
                            _logger.LogTrace($"Building Client Cert List Inventory Item: {c.Alias} Pem: {c.CertChainAsPem}");
                            return BuildInventoryItem(c);
                        }
                        catch 
                        {
                            _logger.LogWarning($"Could not fetch the client certificate: {c?.Alias} associated with description {c?.CertChainAsPem}.");
                            warningFlag = true;
                            return new CurrentInventoryItem();
                        }
                    }).Where(item => item?.Certificates != null).ToList());
                
                if (warningFlag)
                {
                    _logger.LogTrace("Found Warning during Inventory Item Creation");
                    return new JobResult()
                    {
                        Result = OrchestratorJobStatusJobResult.Warning, 
                        JobHistoryId = config.JobHistoryId,
                        FailureMessage = "Could not fetch 1 or more certificates. Refer to the log for more detailed information."
                    };
                }
                
                _logger.MethodExit();

            }
            catch (Exception e1)
            {
                // Status: 2=Success, 3=Warning, 4=Error
                return new JobResult() { Result = OrchestratorJobStatusJobResult.Failure, JobHistoryId = config.JobHistoryId, 
                    FailureMessage = $"Inventory Job Failed During Inventory Item Creation: {e1.Message} - Refer to logs for more detailed information." };
            }

            try
            {
                // Sends inventoried certificates back to KF Command
                _logger.LogTrace("Submitting Inventory To Keyfactor via submitInventory.Invoke");
                submitInventory.Invoke(inventoryItems);
                _logger.LogTrace("Submitted Inventory To Keyfactor via submitInventory.Invoke");
                
                // Status: 2=Success, 3=Warning, 4=Error
                return new JobResult() { Result = OrchestratorJobStatusJobResult.Success, JobHistoryId = config.JobHistoryId };
            }
            catch (Exception e2)
            {
                // ** NOTE: If the cause of the submitInventory.Invoke exception is a communication issue between the Orchestrator server and the Command server, the job status returned here
                //  may not be reflected in Keyfactor Command.
                return new JobResult() { Result = OrchestratorJobStatusJobResult.Failure, JobHistoryId = config.JobHistoryId, 
                    FailureMessage = $"Inventory Job Failed During Inventory Item Submission: {e2.Message} - Refer to logs for more detailed information." };
            }
        }
        
        private CurrentInventoryItem BuildInventoryItem(Certificate cert)
        {
            try
            {
                _logger.MethodEntry();

                var item = new CurrentInventoryItem
                {
                    Alias = cert.Alias,
                    Certificates =  cert.CertChainAsPem,
                    ItemStatus = OrchestratorInventoryItemStatus.Unknown,
                    PrivateKeyEntry = true, // Client certs will have private keys on the camera
                    UseChainLevel = true // TODO: Check this --- Will only ever have 1 single cert
                };

                _logger.MethodExit();
                
                return item;
            }
            catch (Exception e)
            {
                _logger.LogError($"Error Occurred in Inventory.BuildInventoryItem for Client Certificates: {LogHandler.FlattenException(e)}");
                throw;
            }
        }

    }
}