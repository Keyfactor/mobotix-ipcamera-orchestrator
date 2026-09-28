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

The Mobotix IP Camera Orchestrator Extension uses .NET HttpClientHandler credential negotiation when connecting to Mobotix devices over HTTPS.
This allows the orchestrator to automatically negotiate the authentication mechanism required by the camera.
The orchestrator has been validated against Mobotix cameras configured with:

- Basic
- Digest
- Auto

As a result, customer-side changes to camera authentication policies are generally not required.

## Device Onboarding

Cameras are typically provisioned with a self-signed or otherwise untrusted device identity certificate. The
**Use SSL** and **Bypass TLS Validation** certificate store properties together determine what's required to
connect successfully:

| Use SSL | Bypass TLS Validation | Behavior |
| --- | --- | --- |
| `False` | *(any)* | Connects over plain HTTP. There is no TLS handshake, so certificate trust does not apply, and **Bypass TLS Validation** has no effect either way. |
| `True` | `True` | Connects over HTTPS but skips certificate validation entirely - any certificate is accepted, including the camera's untrusted factory certificate. |
| `True` | `False` | Connects over HTTPS and validates the certificate like any other TLS connection - see below for what's required to pass. |

> [!WARNING]
> It is highly recommended to keep **Use SSL** enabled. Plain HTTP sends credentials and certificate data to
> the camera unencrypted, and is only intended as a fallback for cameras or networks that cannot support HTTPS.

When **Use SSL** is `True` and **Bypass TLS Validation** is `False`, install the certificate's issuing
intermediate and root CAs into the orchestrator server's local trust store so that standard TLS validation
succeeds. This applies both to the camera's initial factory certificate and, later, to whatever certificate is
enrolled from the customer's own PKI - each requires its own issuing intermediate and root CAs to be trusted,
since they're typically different CAs.

### Camera-Specific Trust Validation

This only applies when **Use SSL** is `True` and **Bypass TLS Validation** is `False`.

While the camera still has its factory certificate, its SAN reflects the camera's IP address at the time of
manufacture, not wherever it's actually deployed. This causes a name mismatch even when the issuing CAs are
trusted. To resolve this:

- Enter the camera's factory IP address (shown in the camera's web UI at initial setup) as the certificate
  store's **Store Path** value.
- When the certificate's SAN contains that value, and the certificate is otherwise trusted (its issuing
  intermediate and root CAs are installed on the orchestrator server), the connection succeeds despite not
  matching the camera's current network address.
- This doesn't change anything about trust itself: a certificate that doesn't chain to a trusted CA still
  fails, regardless of Store Path.

> [!NOTE]
> This check only compares the certificate's SAN against the recorded Store Path value - it does not verify
> which CA issued the certificate. It is not intended to distinguish the camera's factory certificate from a
> customer-issued certificate that happens to carry the same value, for example due to a misconfigured
> enrollment.

> [!NOTE]
> This only applies while the camera presents its factory certificate. Once a customer-PKI certificate is
> enrolled via ODKG, its SAN should match the address actually used to connect to the camera (the store's
> Client Machine value), and this factory-IP exception no longer comes into play.

> [!IMPORTANT]
> Inventory and Reenrollment (ODKG) jobs both connect to the camera using the same HTTP/HTTPS connection, so
> **Bypass TLS Validation** affects both job types.

## Enrollment Behavior

The following enrollment behaviors are specific to Mobotix cameras and should be considered when designing certificate automation workflows.

### Single Certificate Replacement

Mobotix devices support only a single TLS server certificate for the camera's web server, and the integration manages only that certificate — no others on the device are in scope. Every ODKG job replaces it at the same fixed location and reboots the device to apply the change.

#### Configuration Example

A typical ODKG job configuration for a Mobotix certificate store:

- **Store Path:** the camera's factory IP address *(see [Device Onboarding](#device-onboarding))*
- **Overwrite:** `true` or `false` *(has no effect)*
- **Alias:** *(only shown if Overwrite is checked; has no effect)*

In this configuration:
- The ODKG job generates a new certificate and private key
- They are uploaded to the camera's fixed certificate location (`httpd_cert.pem`), replacing the previous certificate
- The device reboots, after which the new certificate becomes active and is the certificate presented in subsequent TLS sessions with the camera's web server

Operational behavior:
- The **Overwrite** and **Alias** fields have no effect — there is only one certificate slot on the device, and every job always replaces it, regardless of these settings
- **Store Path** is used only for the factory-certificate TLS validation described under [Device Onboarding](#device-onboarding); it does not identify or affect which certificate file is managed on the camera
- Inventory always reports this certificate to Command with a fixed Alias of `HTTPS`, regardless of Store Path or the job-configuration Alias field above

> [!IMPORTANT]
> The camera may take several minutes to reboot and become reachable again. During this time, API calls (such as Inventory jobs) will fail because the device is temporarily unavailable.

> [!TIP]
> Wait for the camera to fully come back online before initiating additional jobs, such as Inventory or ODKG.

### Subject Alternative Names (SANs)

As of Keyfactor Command v25.4, Subject Alternative Names (SANs) can be specified for ODKG jobs. Support for passing SANs to the orchestrator also requires, at minimum, Keyfactor Universal Orchestrator v25.1.

This integration supports DNS, IP, and URI SAN types. Any other SAN type included in the ODKG job stops the job before a certificate is requested, rather than being silently dropped from the enrolled certificate. SANs are not automatically added if none are supplied.

## Troubleshooting

_No known troubleshooting guidance available at this time._

## Operational Notes

Only the end-entity (leaf) certificate is inventoried and enrolled by this integration - intermediate and root
certificates in the issuing chain are not automatically retrieved or bundled. If the leaf's issuing intermediate
and root certificates are separately present in Command (for example, imported independently), Command can still
build and display the complete chain for that certificate. This applies to both Inventory and Reenrollment
(ODKG) jobs.

## Release Notes

**1.0.0**
- Initial Public Version.
- Improved HTTP communication diagnostics to improve troubleshooting of camera connectivity and communication issues.
- Updated ODKG job initialization to align with the other jobs.
- Removed an unnecessary dependency path that could prevent the ODKG job loading in certain environments.
- Added support for PAM credential retrieval.
- Added `Use SSL` and `Bypass TLS Validation` certificate store properties, including support for validating a factory-fresh camera's certificate and reporting specific TLS validation failure reasons.
- Added support for URI Subject Alternative Names, in addition to DNS and IP; unsupported SAN types now stop the enrollment job with a clear error instead of being silently dropped.
- Fixed issues where Inventory could report zero certificates without failing, and where enabling Bypass TLS Validation could cause an unexpected error retrieving the camera's certificate.
