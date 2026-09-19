## Overview

The Mobotix IP Camera Orchestrator extension remotely manages TLS certificates on Mobotix IP Network Cameras (x7 and x8 models). 

This extension provides functionality to:
- Inventory the TLS certificate used by the camera's web server
- Enroll and install a new TLS certificate for use by the camera's web server

Since Mobotix cameras do not support on-device key generation, the extension performs certificate enrollment by:
1. Generating a private key pair in memory on the orchestrator server
2. Creating and submitting a CSR to Command
3. Receiving the issued certificate
4. Uploading the private key and certificate to the camera via REST API
5. Rebooting the device to apply the new certificate

This job type is still referred to as ODKG (On Device Key Generation) in Keyfactor Command — the platform-wide name for a Reenrollment job — even though for Mobotix the key pair is generated on the orchestrator server rather than on the device itself.

This workflow is fully automated. 

### Use Cases

#### Supported

1. Inventory of TLS certificate (web server only)
2. Automated enrollment and installation of TLS certificates

#### Not Supported

1. Upload of certificates outside the enrollment workflow
2. Removal of certificates from the camera
3. Management (add/remove) of CA certificates on the device

## Requirements

1. Out of the box, a Mobotix IP Network Camera will typically have configured an **Administrator** account. It is recommended to create a new account specifically for executing API calls. This account will need \'Administrator\' privileges since the orchestrator extension is capable of making configuration changes, such as enrolling new certificates.
2. Network connectivity from orchestrator to camera over HTTP/HTTPS

### Camera Compatibility

Supported on Mobotix x7 and x8 model cameras (per the Mobotix API).

- **Tested model:** Mobotix v71
- **Tested software version:** MX-V7.3.5.35

Has not been tested with any other model or software version.

### Authentication

The Mobotix IP Camera Orchestrator Extension uses .NET HttpClientHandler credential negotiation when connecting to Mobotix devices over HTTPS, the same mechanism used by the AXIS IP Camera Orchestrator Extension.
This allows the orchestrator to automatically negotiate the authentication mechanism required by the camera.
The orchestrator has been validated against Mobotix cameras configured with:

- Basic
- Digest
- Auto

As a result, customer-side changes to camera authentication policies are generally not required.

## Device Onboarding

Cameras are typically provisioned with a self-signed or otherwise untrusted device identity certificate.

The **Use SSL** certificate store property controls whether the orchestrator connects to the camera over HTTP or
HTTPS:

- If **Use SSL** is disabled, the orchestrator connects over plain HTTP. There is no TLS handshake, so
  certificate trust does not apply, and **Bypass TLS Validation** has no effect.
- If **Use SSL** is enabled, the orchestrator connects over HTTPS and, by default, validates the camera's
  certificate like any other TLS connection - denying the operation if the certificate is not trusted.

> [!WARNING]
> It is highly recommended to keep **Use SSL** enabled. Plain HTTP sends credentials and certificate data to
> the camera unencrypted, and is only intended as a fallback for cameras or networks that cannot support HTTPS.

To connect successfully over HTTPS when the camera's certificate is untrusted, either:

- Install the certificate's issuing intermediate and root CAs into the orchestrator server's local trust store
  so that standard TLS validation succeeds, or
- Enable the **Bypass TLS Validation** certificate store property (see the store type documentation) to skip
  TLS validation.

This trust requirement is not limited to the camera's initial factory certificate. Once a certificate issued by
the customer's own PKI has been enrolled onto the camera, subsequent connections are validated against that
certificate the same way - so the customer PKI's issuing intermediate and root CAs must also be installed in
the orchestrator server's local trust store, unless **Bypass TLS Validation** is used instead.

> [!IMPORTANT]
> Inventory and Reenrollment (ODKG) jobs both connect to the camera using the same HTTP/HTTPS connection, so
> **Bypass TLS Validation** affects both job types.

## Enrollment Behavior

The following enrollment behaviors are specific to Mobotix cameras and should be considered when designing certificate automation workflows.

### Single Certificate Replacement

Mobotix devices support only a single TLS server certificate for the camera's web server, and the integration manages only that certificate — no others on the device are in scope. Every ODKG job replaces it at the same fixed location and reboots the device to apply the change.

#### Configuration Example

A typical ODKG job configuration for a Mobotix certificate store:

- **Store Path:** `httpd_cert.pem` *(automatically set by the store type configuration)*
- **Overwrite:** `true` or `false` *(has no effect)*
- **Alias:** *(only shown if Overwrite is checked; has no effect)*

In this configuration:
- The ODKG job generates a new certificate and private key
- They are uploaded to the camera's fixed certificate location (`httpd_cert.pem`), replacing the previous certificate
- The device reboots, after which the new certificate becomes active and is the certificate presented in subsequent TLS sessions with the camera's web server

Operational behavior:
- The **Overwrite** and **Alias** fields have no effect — there is only one certificate slot on the device, and every job always replaces it, regardless of these settings
- **Store Path** is fixed to `httpd_cert.pem` by the store type configuration; the integration does not use this value, but it identifies which certificate file is being tracked

> [!IMPORTANT]
> The camera may take several minutes to reboot and become reachable again. During this time, API calls (such as Inventory jobs) will fail because the device is temporarily unavailable.

> [!TIP]
> Wait for the camera to fully come back online before initiating additional jobs, such as Inventory or ODKG.

### Subject Alternative Names (SANs)

As of Keyfactor Command v25.4, Subject Alternative Names (SANs) can be specified for ODKG jobs. Support for passing SANs to the orchestrator also requires, at minimum, Keyfactor Universal Orchestrator v25.1.

The Mobotix API only supports DNS and IP SAN types. Any other SAN types included in the ODKG job will be ignored and will not be added to the enrolled certificate. SANs are not automatically added if none are supplied.

## Troubleshooting

_No known troubleshooting guidance available at this time._

## Operational Notes

_No known operational limitations or version-specific notes at this time._

## Release Notes

**1.0.0**
- Improved HTTP communication diagnostics to improve troubleshooting of camera connectivity and communication issues.
- Updated ODKG job initialization to align with the other jobs.
- Removed an unnecessary dependency path that could prevent the ODKG job loading in certain environments.
- Added support for PAM credential retrieval.
- Initial Public Version.
