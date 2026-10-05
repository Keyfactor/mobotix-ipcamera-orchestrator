// Copyright 2026 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Keyfactor.Logging;
using Microsoft.Extensions.Logging;

namespace Keyfactor.Extensions.Orchestrator.MobotixIPCamera.Helpers
{
    public static class SANBuilder
    {
        /// <summary>
        /// Creates a list of SANs in the format "dns:[dns value]", "ip:1.1.1.1", etc.
        /// </summary>
        public static List<string> BuildSANList(Dictionary<string, string[]> sans, ILogger logger)
        {
            logger.MethodEntry();
            
            logger.LogTrace($"Building SAN list from dictionary");
            
            var parts = new List<string>();
            
            if (sans == null || sans.Count == 0)
            {
                logger.LogTrace($"SANs is null or empty");
                return parts;
            }

            foreach (var entry in sans)
            {
                string key = NormalizeSanKey(entry.Key);

                if (key is not ("DNS" or "IP" or "URI"))
                    continue;

                if (entry.Value == null || entry.Value.Length == 0)
                    continue;
                
                // NOTE: We are separating the key and value pairs with a colon because this is the format
                // required to send SANs to the API endpoint
                parts.AddRange(
                    entry.Value
                        .Where(v => !string.IsNullOrWhiteSpace(v))
                        .Select(v => $"{key}:{v.Trim()}")
                );
            }
            
            logger.MethodExit();

            return parts;
        }

        /// <summary>
        /// Creates a normalized dictionary of SANs
        /// </summary>
        public static IDictionary<string, string[]> NormalizeSANs(IDictionary<string, string[]>? sans)
        {
            var normalized = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            
            if (sans == null || sans.Count == 0)
            {
                return normalized;
            }
            
            foreach (var entry in sans)
            {
                string key = NormalizeSanKey(entry.Key);

                if (entry.Value.Length == 0)
                    continue;

                normalized[key] = entry.Value
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Select(v => v.Trim())
                    .ToArray();
            }

            return normalized;

        }

        // SAN types this integration currently supports enrolling on Mobotix cameras. Not a limitation
        // of the camera itself - the camera's upload API never inspects certificate contents at all -
        // this is purely a scope decision for what this integration builds into the CSR.
        private static readonly HashSet<string> SupportedSanTypes = new(StringComparer.OrdinalIgnoreCase) { "DNS", "IP", "URI" };

        /// <summary>
        /// Rejects any SAN type outside what this integration currently supports for Mobotix cameras.
        /// Unsupported types are not silently dropped - this throws so CSR generation fails outright,
        /// rather than issuing a certificate missing a SAN the caller explicitly requested.
        /// </summary>
        public static void ApplyCameraSanTypeLimitations(IDictionary<string, string[]> sans, ILogger logger)
        {
            foreach (var key in sans.Keys)
            {
                if (!SupportedSanTypes.Contains(key))
                {
                    logger.LogWarning($"Unsupported SAN type for this integration: {key}");
                    throw new ArgumentException($"Unsupported SAN type: {key}");
                }
            }
        }

        /// <summary>
        /// Normalize SAN type keys to RFC-compliant names.
        /// Courtesy of B.Pokorny.
        /// </summary>
        private static string NormalizeSanKey(string key)
        {
            return key.Trim().ToLower() switch
            {
                "dns" => "DNS",
                "ip" or "ip4" or "ip6" => "IP",
                "uri" => "URI",
                _ => key.ToLower() // default
            };
        }
    }
}