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

## Certificate Behavior and Constraints

Due to device limitations:

- Only a single TLS certificate is managed per device
- Certificate and private key are uploaded separately
- A **device reboot is required** for the certificate to take effect (Handled automatically via the enrollment workflow)

## Post Installation

Work in Progress

## Caveats

> [!NOTE]
> **v1.0.0**
> - Only one certificate is managed at a time
> - ODKG/Reenrollment jobs must use the same alias (i.e. "HTTPS") and "Overwrite" must be set to *true* 