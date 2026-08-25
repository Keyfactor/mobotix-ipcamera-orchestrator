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
                
                // TODO: Any certificate constraint for Mobotix TLS cert, put here ---
                if (key is not ("DNS" or "IP"))
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
                
                // TODO: Any certificate constraint for Mobotix TLS cert, put here ---
                if (key is not ("DNS" or "IP"))
                    continue;
                
                if (entry.Value == null || entry.Value.Length == 0)
                    continue;

                normalized[key] = entry.Value
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Select(v => v.Trim())
                    .ToArray();
            }

            return normalized;

        }

        /// <summary>
        /// Normalize SAN type keys to RFC-compliant names.
        /// **NOTE: The Axis API only supports the addition of 'dns' and 'ip' SAN types.
        /// Courtesy of B.Pokorny.
        /// </summary>
        private static string NormalizeSanKey(string key)
        {
            return key.Trim().ToLower() switch
            {
                "dns" => "DNS",
                "ip" or "ip4" or "ip6" => "IP",
                _ => key.ToLower() // default
            };
        }
    }
}