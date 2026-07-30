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

This workflow is fully automated. 

> [!NOTE]
> Mobotix devices support only a single TLS certificate for the camera's web server.
> As a result, there is only one valid **Store Path** per device, which should
> typically be set to `HTTPS`. Choose a name for the **Store Path** that meaningfully represents the TLS certificate.
>
> During an ODKG/Reenrollment job, a new certificate is generated and installed
> on the device, always replacing the existing certificate.
>
> The Store Path value is always used to identify the TLS certificate associated with the camera's web server.
> The **Overwrite** setting and **Alias** field are user interface elements only
> and do not affect how the certificate is installed or managed.

### Configuration Example

The following example demonstrates how the Store Path behaves in an ODKG/Reenrollment job configuration:

- **Store Path:** `HTTPS`
- **Overwrite:** `true` or `false`
- **Alias:** *(ignored; may or may not be visible)

In this configuration:
- The device supports only a single TLS certificate
- The Store Path (`HTTPS`) represents the camera’s web server TLS endpoint
- The ODKG job generates a new certificate and installs it on the device
- The newly issued certificate always replaces the existing certificate

User interface behavior:
- Selecting **Overwrite** displays the Alias field
- Clearing **Overwrite** hides the Alias field

Operational behavior:
- The **Overwrite** setting has no impact on certificate replacement
- The **Alias** value is not used for certificate identification
- The Store Path is the only value used to identify the certificate associated with the camera

> [!TIP]
> Because only a single TLS certificate exists per device, there is only one Store Path.
> Use a consistent, meaningful value (for example, `HTTPS`).

### Use Cases

#### Supported

1. Inventory of TLS certificate (web server only)
2. Automated enrollment and installation of TLS certificates

#### Not Supported

1. Upload of certificates outside the enrollment workflow
2. Removal of certificates from the camera
3. Management (add/remove) of CA certificates on the device

## Requirements

1. A Mobotix IP Network Camera (tested on MX-V7.3.5.35)
2. An account with **Administrator** privileges
3. Network connectivity from orchestrator to camera over HTTP/HTTPS
4. The Mobotix IP Camera integration currently supports **Basic Authentication only**, which must be configured on the camera.

## Certificate Behavior and Constraints

Due to device limitations:

- Only a single TLS certificate is managed per device
- Certificate and private key are uploaded separately
- A **device reboot is required** for the certificate to take effect (handled automatically via the enrollment workflow)

## Post Installation

After a certificate is installed as part of an ODKG/Reenrollment job, the camera
is rebooted to apply the new TLS certificate.

> [!IMPORTANT]
> The camera may take several minutes to reboot and become reachable again.
> During this time, API calls (such as Inventory jobs) will fail because the device
> is temporarily unavailable.

> [!TIP]
> It is recommended to wait for the camera to fully come back online before initiating
> additional jobs, such as Inventory or ODKG/Reenrollment.

## Caveats

> [!NOTE]
> **v1.0.0**
> - Only one certificate is managed at a time
> - ODKG/Reenrollment jobs must use the same alias (derived from Store Path, typically `HTTPS`) 

## Release Notes

**1.0.0**
- Improved HTTP communication diagnostics to improve troubleshooting of camera connectivity and communication issues.
- Updated ODKG job initialization to align with the other jobs.
- Removed an unnecessary dependency path that could prevent the ODKG job loading in certain environments.
- Added support for PAM credential retrieval.
- Initial Public Version.