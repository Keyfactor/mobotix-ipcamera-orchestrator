## Overview

The Mobotix IP Camera certificate store type represents the TLS configuration of a Mobotix IP network camera, including
the certificate and private key used by the camera's web server.

Only a single TLS certificate can be active on the camera at any time.
Updating the certificate replaces the existing one.
The default certificate installed on the camera is the factory device ID certificate.

## Requirements

1. A user Account with \'Administrator\' privileges (Basic Authentication)
2. Camera IP address (and possible port number)


> [!NOTE]
> As of Keyfactor Command v25.4, SANs can be provided for a Reenrollment (ODKG) job.
> You must also have installed, at minimum, the Keyfactor Universal Orchestator v25.1
> in order for the SANs to be sent to the orchestrator.
>
> The Mobotix API supports only DNS and IP SANs. Other SAN types will be ignored
