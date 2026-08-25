// Copyright 2026 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;

using Keyfactor.Logging;
using Keyfactor.Orchestrators.Extensions;
using Keyfactor.Extensions.Orchestrator.MobotixIPCamera.Model;
using Keyfactor.Orchestrators.Extensions.Interfaces;
using Keyfactor.Extensions.Orchestrator.MobotixIPCamera.Helpers;
using Keyfactor.Extensions.Orchestrator.MobotixIPCamera.Exceptions;

/* MobotixHttpClient.cs
 * ---------------------------------------------------------------------------------------------------
 * Description:
 * This class serves as an interface to make HTTP requests to the Mobotix camera web server and receive/parse responses.
 * The requests made are those required for implementation of the following orchestrator jobs:
 * -Inventory
 * -Reenrollment (ODKG)
 *
 * Notes:
 * This integration manages only the TLS certificate.
 *
 */
namespace Keyfactor.Extensions.Orchestrator.MobotixIPCamera.Client
{
    public class MobotixHttpClient
    {
        private readonly RestClient _httpClient;
        private string _baseRestClientUrl;
        private X509Certificate2 _capturedTlsCert;
        
        private ILogger Logger { get; }
        public MobotixHttpClient(JobConfiguration config, CertificateStore store, IPAMSecretResolver resolver)
        {
            try
            {
                var errorContext = new CertificateErrorContext();
                
                Logger = LogHandler.GetClassLogger<MobotixHttpClient>();
                Logger.LogTrace("Entered MobotixHttpClient constructor.");
                Logger.LogTrace("Initializing Mobotix IP Camera HTTP client");
                
                // ** NOTE: Ignoring the default config.UseSSL custom field --- we will always connect to the device via HTTPS
                _baseRestClientUrl = $"https://{store.ClientMachine}";
                
                Logger.LogDebug($"Base HTTP client URL: {_baseRestClientUrl}");
                
                // Retrieve username and password credentials to connect to the device
                Logger.LogTrace("Adding device credentials to the HTTP client options...");
                string username = PAMUtilities.ResolvePAMField(resolver, Logger, "API Username", config.ServerUsername);
                string password = PAMUtilities.ResolvePAMField(resolver, Logger, "API Password", config.ServerPassword);
                
#if DEBUG
                Logger.LogTrace($"API Username: {username}");
                Logger.LogTrace($"API Password: {password}");
#endif
                
                // The client intentionally uses HttpClientHandler credentials
                // rather than RestSharp's HttpBasicAuthenticator to allow
                // automatic negotiation of Basic vs Digest authentication
                // based on the authentication challenge presented by the camera.
                Logger.LogInformation($"Adding custom TLS cert validator to the HTTP client options.");
                Logger.LogTrace($"Using HttpClientHandler credential negotiation for camera authentication.");
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        DeviceCertValidator.GetValidator(
                            store.StorePath, 
                            errorContext, 
                            Logger, 
                cert => _capturedTlsCert = cert),
                    
                    Credentials = new NetworkCredential(username, password),
                    
                    PreAuthenticate = false // PreAuthenticate is set to false to avoid the default behavior of sending the username and password in the Authorization header
                };
                
                // Initialize HTTP client options with the base URL and custom TLS cert validator
                RestClientOptions options = new RestClientOptions(_baseRestClientUrl)
                {
                    ConfigureMessageHandler = _ => handler
                };

                // Add SSL validation
                Logger.LogTrace("Validating connection to the device...");

                _httpClient = new RestClient(options);
                // Initiates the TLS handshake to retrieve the server cert
                var request = new RestRequest("config/camera/media"); 
                var response = _httpClient.Execute(request);

                // TODO: Build the list of errors to log to the console
                /*StringBuilder errorSb = new StringBuilder();
                if (errorContext.HasErrors)
                {
                    foreach (var error in errorContext.Errors)
                    {
                        errorSb.AppendLine(error);
                    }
                    throw new Exception(errorSb.ToString());
                }*/
                
                // Log the WWW-Authenticate headers if 401 Unauthorized returned
                // Throw exception if connection cannot be made successfully to the camera
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Logger.LogWarning("Camera returned 401 Unauthorized");

                    foreach (var header in response.Headers)
                    {
                        Logger.LogDebug($"WWW-Authenticate header: {header.Value}");
                    }

                    throw new AuthenticationException(
                        "Authentication to the Mobotix camera failed due to 401 Unauthorized. Verify the configured credentials.");
                }

                if (!response.IsSuccessful)
                {
                    throw new Exception(response.ErrorMessage);
                }
                
                Logger.LogTrace($"Connection to the device response status code: {response.StatusCode}");
                Logger.LogTrace("Completed Initialization of Mobotix IP Camera HTTP Client");
                Logger.LogTrace("Leaving MobotixHttpClient constructor.");
            }
            catch (DeviceCertValidationException ex)
            {
                Logger.LogError("Device TLS cert validation failed while connecting to the device: " + LogHandler.FlattenException(ex));
                throw new Exception(ex.Message);
            }
            catch (AuthenticationException ex1)
            {
                Logger.LogError("Authentication to the device failed: " + LogHandler.FlattenException(ex1));
                throw new Exception(ex1.Message);
            }
            catch (HttpRequestException ex2)
            {
                Logger.LogError("Failed to communicate with the device: " + LogHandler.FlattenException(ex2));
                throw new Exception(ex2.Message);
            }
            catch (Exception ex3)
            {
                Logger.LogError("Unexpected error while connecting to the device: " + LogHandler.FlattenException(ex3));
                throw new Exception(ex3.Message);
            }
        }
        
        // Business Logic for Orchestrator Jobs
        
        /// <summary>
        /// Retrieve the TLS certificate on the device.
        /// NOTE: An uploaded certificate with the name "httpd_cert.pem" is automatically associated with the camera web interface.
        /// The contents of that PEM file correspond to the TLS certificate.
        /// </summary>
        /// <returns>CertificateData object</returns>
        public CertificateData ListCertificates(string alias)
        {  
            Logger.MethodEntry();
            
            var certsFound = new CertificateData();
            
            try
            {
                var getCertsResource = $"config/camera/media";
                var queryParameters = new Dictionary<string, string> { { "file", "httpd_cert.pem" } };
                var httpResponse = ExecuteHttp(getCertsResource, Method.Get, queryParameters);

                // Decode the HTTP response if failed
                if (httpResponse is {IsSuccessful:false})
                {
                    Logger.LogError($"HTTP Request unsuccessful - HTTP Response: {DecodeHttpStatus(httpResponse)}");
                    throw new Exception($"HTTP Request unsuccessful.");
                }
                
                // Decode the API response when HTTP response is successful
                if (httpResponse != null && string.IsNullOrEmpty(httpResponse.Content))
                {
                    throw new Exception("No content returned from HTTP Response");
                }
                
                // Parse response
                var cameraMediaCertResult = HTTPResponseParser.ParseDownloadResponse(httpResponse.RawBytes, _baseRestClientUrl);

                if (cameraMediaCertResult.HasCertificate)
                {
                    Logger.LogDebug($"Retrieved TLS Certificate from Device");
                    var certChain = new List<string>();
                    foreach (var c in cameraMediaCertResult.CertChain)
                    {
                        Logger.LogDebug($"Adding Certificate to Chain Model: {c}");
                        certChain.Add(c);
                    }

                    certsFound.Certs.Add( new Certificate() {Alias = alias, CertChainAsPem = certChain} );
                }
                else
                {
                    // Check what the TLS cert was captured
                    if (_capturedTlsCert != null)
                    {
                        Logger.LogWarning($"TLS Certificate was captured. TLS Certificate: {_capturedTlsCert.Subject}");
                        certsFound.Certs.Add( new Certificate() {Alias = "HTTPS", CertChainAsPem = new List<string>() {Certificate.ExportToPem(_capturedTlsCert)}} );
                    }
                    else
                    {
                        Logger.LogWarning("No TLS Certificate was captured. Please check the logs for more information.");   
                    }
                }
                
                Logger.MethodExit();
                
                return certsFound;
            }
            catch (Exception e)
            {
                Logger.LogError("Error retrieving client certificates on device: " + LogHandler.FlattenException(e));
                throw;
            }
        }

        /// <summary>
        /// Posts a PEM file (one for certificate, one for private key) to the device for TLS connections.
        /// </summary>
        /// <param name="pemContent">Base-64 encoded certificate or private key contents</param>
        /// <param name="fileName">Filename to indicate certificate or private key</param>
        public void UploadPemFile(string pemContent, string fileName)
        {
            Logger.MethodEntry();

            try
            {
                var postCertResource = $"config/camera/media";
                var request = new RestRequest(postCertResource, Method.Post);
                
                Logger.LogDebug($"Preparing multipart/form-data upload for file {fileName}");
                
                var (body, contentType) = BuildMultipart("some_file",fileName,"application/x-x509-ca-cert",pemContent);

                Logger.LogDebug($"Adding request headers: Content-Type: {contentType}; Accept: */*");
                request.AddHeader("Content-Type", contentType);
                request.AddHeader("Accept", "*/*");
                
                Logger.LogDebug($"Adding request body");
                request.AddStringBody(body, DataFormat.None);
                
                Logger.LogTrace($"Executing upload request for '{fileName}'");
                var httpResponse = ExecuteHttp(request);
                
                // Decode the HTTP response if failed
                if (httpResponse is {IsSuccessful:false})
                {
                    Logger.LogError($"HTTP Request unsuccessful - HTTP Response: {DecodeHttpStatus(httpResponse)}");
                    throw new Exception($"HTTP Request unsuccessful.");
                }
                
                // Decode the API response when HTTP response is successful
                if (httpResponse != null && string.IsNullOrEmpty(httpResponse.Content))
                {
                    throw new Exception("No content returned from HTTP Response");
                }
                
                Logger.LogInformation($"{fileName} upload completed successfully");
                
                Logger.MethodExit();
            }
            catch (Exception e)
            {
                Logger.LogError("Error uploading client certificate to device: " + LogHandler.FlattenException(e));
                throw;
            }
        }
        
        /// <summary>
        /// Restart the device to apply configuration changes.
        /// </summary>
        public void RebootDevice()
        {  
            Logger.MethodEntry();
            
            try
            {
                var postRebootResource = $"admin/rcontrol";
                var queryParameters = new Dictionary<string, string> { { "action", "reboot" } };
                var httpResponse = ExecuteHttp(postRebootResource, Method.Post, queryParameters);

                // Decode the HTTP response if failed
                if (httpResponse is {IsSuccessful:false})
                {
                    Logger.LogError($"HTTP Request unsuccessful - HTTP Response: {DecodeHttpStatus(httpResponse)}");
                    throw new Exception($"HTTP Request unsuccessful.");
                }
                
                // Decode the API response when HTTP response is successful
                if (httpResponse != null && string.IsNullOrEmpty(httpResponse.Content))
                {
                    throw new Exception("No content returned from HTTP Response");
                }
                
                // Parse response
                if (!httpResponse.Content.Contains("OK - Reboot"))
                {
                    throw new Exception("Device did not acknowledge reboot");
                }
                
                Logger.MethodExit();
            }
            catch (Exception e)
            {
                Logger.LogError("Error rebooting device: " + LogHandler.FlattenException(e));
                throw;
            }
        }
        
        private RestResponse ExecuteHttp(string resource, Method httpMethod, Dictionary<string, string>? queryParams = null)
        {
            try
            {
                Logger.MethodEntry();

                // Check if the HTTP client was properly initialized
                if (_httpClient is null)
                {
                    throw new Exception("Mobotix IP Camera HTTP Client was not initialized.");
                }

                var request = new RestRequest(resource, httpMethod);
                
                // Add query parameters, if any
                if (queryParams != null)
                {
                    foreach (var p in queryParams)
                    {
                        request.AddQueryParameter(p.Key, p.Value);
                    }
                }

                Logger.LogDebug($"HTTP Request URI: {_httpClient.BuildUri(request)}");
                Logger.LogDebug($"HTTP Method: {httpMethod.ToString()}");

                Logger.LogTrace("Executing REST Request...");
                var httpResponse = _httpClient.Execute(request);
                if (httpResponse is null)
                {
                    throw new InvalidOperationException();
                }

                Logger.LogTrace("HTTP Request completed");

                Logger.LogDebug($"HTTP Response: {httpResponse?.Content}");

                Logger.MethodExit();

                return httpResponse;
            }
            catch (Exception e)
            {
                Logger.LogError($"Error Occured in MobotixRestClient.ExecuteHttp: {LogHandler.FlattenException(e)}");
                throw;
            }
        }
        
        private RestResponse ExecuteHttp(RestRequest request)
        {
            Logger.MethodEntry();

            try
            {
                // Check if the HTTP client was properly initialized
                if (_httpClient is null)
                {
                    throw new Exception("Mobotix IP Camera HTTP Client was not initialized.");
                }

                Logger.LogDebug($"HTTP Request URI: {_httpClient.BuildUri(request)}");
                Logger.LogDebug($"HTTP Method: {request.Method.ToString()}");

                Logger.LogTrace("Executing REST Request...");
                var httpResponse = _httpClient.Execute(request);
                if (httpResponse is null)
                {
                    throw new InvalidOperationException();
                }

                Logger.LogTrace("HTTP Request completed");
                
                Logger.LogDebug($"HTTP Response: {httpResponse?.Content}");
               
                Logger.MethodExit();

                return httpResponse;
            }
            catch (Exception e)
            {
                Logger.LogError($"Error Occured in MobotixRestClient.ExecuteHttp: {LogHandler.FlattenException(e)}");
                throw;
            }
        }
        
        /// <summary>
        /// Decodes the given HTTP status code into a human-readable string.
        /// These include some of the most common HTTP status codes.
        /// </summary>
        /// <param name="httpResponse"></param>
        /// <returns>Human-readable representation of the HTTP status code</returns>
        private string DecodeHttpStatus(RestResponse httpResponse)
        {
            Logger.MethodEntry();
            
            Logger.LogDebug($"HTTP Response Code: {httpResponse.StatusCode}");
            var codeString = "";
            switch (httpResponse.StatusCode)
            {
                case HttpStatusCode.BadRequest:
                {
                    codeString = "Bad Request! (400)";
                    break;
                }
                case HttpStatusCode.Unauthorized:
                {
                    codeString = "Unauthorized! (401)";
                    break;
                }
                case HttpStatusCode.Forbidden:
                {
                    codeString = "Forbidden! (403)";
                    break;
                }
                case HttpStatusCode.NotFound:
                {
                    codeString = "Not Found! (404)";
                    break;
                }
                case HttpStatusCode.InternalServerError:
                {
                    codeString = "Internal Server Error! (500)";
                    break;
                }
                default:
                {
                    codeString = "No response received! Possible causes: Timeouts, no network connectivity, DNS resolution failure, SSL issues, firewall configuration, etc.";

                    codeString += $"\nResponse Status: {httpResponse.ResponseStatus}";
                    
                    if (!string.IsNullOrEmpty(httpResponse.ErrorMessage))
                    {
                        codeString += $"\nError message: {httpResponse.ErrorMessage}";
                    }
                    
                    if (httpResponse.ErrorException != null)
                    {
                        codeString += $"\nException: {httpResponse.ErrorException}";
                    }
                    break;
                }
            }

            Logger.MethodExit();
            
            return codeString;
        }

        /// <summary>
        /// Builds a multipart/form-data request body manually.
        ///
        /// WHY do this manually:
        /// 1. RestSharp auto-generates multipart requests, but:
        /// -- It adds quotes around the boundary (breaks with some devices)
        /// -- It may format parts differently than expected
        /// 2. Embedded devices (like cameras) often require VERY STRICT formatting
        /// 3. This guarantees a byte-for-byte compatible format
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="fileName"></param>
        /// <param name="contentType"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        private (string Body, string ContentType) BuildMultipart(string fieldName, string fileName, string contentType, string pemContent)
        {
            Logger.MethodEntry();
            
            // Generate a unique boundary string
            // WHY do this:
            // 1. Boundary separates parts in multipart payloads
            // 2. Must be unique so it does not accidentally appear in the file content
            // 3. Prefix it with dashes for readability and compatibility
            var boundary = "----------------" + Guid.NewGuid().ToString("N");
            
            var body = new StringBuilder();
            
            // Start the multipart section
            // WHY do this:
            // 1. Every part MUST BEGIN with: --<boundary>
            // 2. This is how the camera knows a new part is starting
            body.AppendLine($"--{boundary}");
            
            // Content-Disposition header defines:
            // 1. Form field name (must EXACTLY match API expectations)
            // 2. Filename (some devices validate this)
            body.AppendLine($"Content-Disposition: form-data; name=\"{fieldName}\"; filename=\"{fileName}\"");

            // Content-Type for THIS part (not the whole request!)
            // WHY do this:
            // 1. Tells the camera what kind of file this is
            // 2. The Mobotix camera requires a specific value (e.g. application/x-x509-ca-cert)
            body.AppendLine($"Content-Type: {contentType}");

            // VERY IMPORTANT: Blank line between headers and content
            // WHY do this:
            // 1. Required by HTTP spec
            // 2. Signals end of headers, start of body
            // 3. Missing this will cause the device to ignore the file silently
            body.AppendLine();

            // Actual file content (PEM)
            // WHY do this:
            // 1. This is what the camera will process as the uploaded file
            // 2. Should not be altered or reformatted
            body.AppendLine(pemContent);

            // End the multipart section
            // WHY do this:
            // 1. "--boundary--" indicates FINAL boundary (end of request body; remember NO QUOTES)
            // 2. Without this, server may treat request as incomplete
            body.AppendLine($"--{boundary}--");
            
            Logger.MethodExit();
            
            // Return the body and content type
            return (body.ToString(), $"multipart/form-data; boundary={boundary}");
        }
    }
}