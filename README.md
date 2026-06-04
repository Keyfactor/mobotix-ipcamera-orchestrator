<h1 align="center" style="border-bottom: none">
    Mobotix IP Camera Universal Orchestrator Extension
</h1>

<p align="center">
  <!-- Badges -->
<img src="https://img.shields.io/badge/integration_status-pilot-3D1973?style=flat-square" alt="Integration Status: pilot" />
<a href="https://github.com/Keyfactor/mobotix-ipcamera-orchestrator/releases"><img src="https://img.shields.io/github/v/release/Keyfactor/mobotix-ipcamera-orchestrator?style=flat-square" alt="Release" /></a>
<img src="https://img.shields.io/github/issues/Keyfactor/mobotix-ipcamera-orchestrator?style=flat-square" alt="Issues" />
<img src="https://img.shields.io/github/downloads/Keyfactor/mobotix-ipcamera-orchestrator/total?style=flat-square&label=downloads&color=28B905" alt="GitHub Downloads (all assets, all releases)" />
</p>

<p align="center">
  <!-- TOC -->
  <a href="#support">
    <b>Support</b>
  </a>
  ·
  <a href="#installation">
    <b>Installation</b>
  </a>
  ·
  <a href="#license">
    <b>License</b>
  </a>
  ·
  <a href="https://github.com/orgs/Keyfactor/repositories?q=orchestrator">
    <b>Related Integrations</b>
  </a>
</p>

## Overview

The Sample Orchestrator Store utilizes the SOSApi included in this solution. To utilize SOSApi and download the solution. 
Build the SOSApi project and use the compiled binary to host the API/Web interface.



## Compatibility

This integration is compatible with Keyfactor Universal Orchestrator version 25.4 and later.

## Support
The Mobotix IP Camera Universal Orchestrator extension is supported by Keyfactor. If you require support for any issues or have feature request, please open a support ticket by either contacting your Keyfactor representative or via the Keyfactor Support Portal at https://support.keyfactor.com.

> If you want to contribute bug fixes or additional enhancements, use the **[Pull requests](../../pulls)** tab.

## Requirements & Prerequisites

Before installing the Mobotix IP Camera Universal Orchestrator extension, we recommend that you install [kfutil](https://github.com/Keyfactor/kfutil). Kfutil is a command-line tool that simplifies the process of creating store types, installing extensions, and instantiating certificate stores in Keyfactor Command.


The Orchestrator needs to be able to access the IP SOSApi is running under (0.0.0.0/8080 by default).


## MobotixIPCamera Certificate Store Type

To use the Mobotix IP Camera Universal Orchestrator extension, you **must** create the MobotixIPCamera Certificate Store Type. This only needs to happen _once_ per Keyfactor Command instance.



This is a Sample Orchestrator Extension with an implemented Sample Orchestrator Store. 
The main purpose is to provide an easy to install and use orchestrator extension for educational and testing purposes, as well as to provide a reference for development and an assembly of best practices. The current version supports discovery, inventory, management and reenrollment for the custom cert store store type, and includes a basic test framework.

The Sample Orchestrator Store functions using the Sample Orchestrator Store API (SOSApi). This system hosts a basic server that provides an API as well as a web app that allows you to manage this store.

The guidelines on SOSApi use are available at [SOSApi Guide](docs/SOSApi.md).




#### Supported Operations

| Operation    | Is Supported                                                                                                           |
|--------------|------------------------------------------------------------------------------------------------------------------------|
| Add          | 🔲 Unchecked        |
| Remove       | ✅ Checked     |
| Discovery    | 🔲 Unchecked  |
| Reenrollment | ✅ Checked |
| Create       | 🔲 Unchecked     |

#### Store Type Creation

##### Using kfutil:
`kfutil` is a custom CLI for the Keyfactor Command API and can be used to create certificate store types.
For more information on [kfutil](https://github.com/Keyfactor/kfutil) check out the [docs](https://github.com/Keyfactor/kfutil?tab=readme-ov-file#quickstart)
   <details><summary>Click to expand MobotixIPCamera kfutil details</summary>

   ##### Using online definition from GitHub:
   This will reach out to GitHub and pull the latest store-type definition
   ```shell
   # Mobotix IP Camera
   kfutil store-types create MobotixIPCamera
   ```

   ##### Offline creation using integration-manifest file:
   If required, it is possible to create store types from the [integration-manifest.json](./integration-manifest.json) included in this repo.
   You would first download the [integration-manifest.json](./integration-manifest.json) and then run the following command
   in your offline environment.
   ```shell
   kfutil store-types create --from-file integration-manifest.json
   ```
   </details>


#### Manual Creation
Below are instructions on how to create the MobotixIPCamera store type manually in
the Keyfactor Command Portal
   <details><summary>Click to expand manual MobotixIPCamera details</summary>

   Create a store type called `MobotixIPCamera` with the attributes in the tables below:

   ##### Basic Tab
   | Attribute | Value | Description |
   | --------- | ----- | ----- |
   | Name | Mobotix IP Camera | Display name for the store type (may be customized) |
   | Short Name | MobotixIPCamera | Short display name for the store type |
   | Capability | MobotixIPCamera | Store type name orchestrator will register with. Check the box to allow entry of value |
   | Supports Add | 🔲 Unchecked |  Indicates that the Store Type supports Management Add |
   | Supports Remove | ✅ Checked | Check the box. Indicates that the Store Type supports Management Remove |
   | Supports Discovery | 🔲 Unchecked |  Indicates that the Store Type supports Discovery |
   | Supports Reenrollment | ✅ Checked |  Indicates that the Store Type supports Reenrollment |
   | Supports Create | 🔲 Unchecked |  Indicates that the Store Type supports store creation |
   | Needs Server | ✅ Checked | Determines if a target server name is required when creating store |
   | Blueprint Allowed | 🔲 Unchecked | Determines if store type may be included in an Orchestrator blueprint |
   | Uses PowerShell | 🔲 Unchecked | Determines if underlying implementation is PowerShell |
   | Requires Store Password | 🔲 Unchecked | Enables users to optionally specify a store password when defining a Certificate Store. |
   | Supports Entry Password | 🔲 Unchecked | Determines if an individual entry within a store can have a password. |

   The Basic tab should look like this:

   ![MobotixIPCamera Basic Tab](docsource/images/MobotixIPCamera-basic-store-type-dialog.png)

   ##### Advanced Tab
   | Attribute | Value | Description |
   | --------- | ----- | ----- |
   | Supports Custom Alias | Required | Determines if an individual entry within a store can have a custom Alias. |
   | Private Key Handling | Forbidden | This determines if Keyfactor can send the private key associated with a certificate to the store. Required because IIS certificates without private keys would be invalid. |
   | PFX Password Style | Default | 'Default' - PFX password is randomly generated, 'Custom' - PFX password may be specified when the enrollment job is created (Requires the Allow Custom Password application setting to be enabled.) |

   The Advanced tab should look like this:

   ![MobotixIPCamera Advanced Tab](docsource/images/MobotixIPCamera-advanced-store-type-dialog.png)

   > For Keyfactor **Command versions 24.4 and later**, a Certificate Format dropdown is available with PFX and PEM options. Ensure that **PFX** is selected, as this determines the format of new and renewed certificates sent to the Orchestrator during a Management job. Currently, all Keyfactor-supported Orchestrator extensions support only PFX.

   ##### Custom Fields Tab
   Custom fields operate at the certificate store level and are used to control how the orchestrator connects to the remote target server containing the certificate store to be managed. The following custom fields should be added to the store type:

   | Name | Display Name | Description | Type | Default Value/Options | Required |
   | ---- | ------------ | ---- | --------------------- | -------- | ----------- |
   | ServerUsername | Server Username | Enter the username of the configured "service" user on the camera | Secret |  | ✅ Checked |
   | ServerPassword | Server Password | Enter the password of the configured "service" user on the camera | Secret |  | ✅ Checked |
   | ServerUseSsl | Use SSL | Select True or False depending on if SSL (HTTPS) should be used to communicate with the camera. This should always be "True" | Bool | true | ✅ Checked |

   The Custom Fields tab should look like this:

   ![MobotixIPCamera Custom Fields Tab](docsource/images/MobotixIPCamera-custom-fields-store-type-dialog.png)


   ###### Server Username
   Enter the username of the configured "service" user on the camera


   > [!IMPORTANT]
   > This field is created by the `Needs Server` on the Basic tab, do not create this field manually.




   ###### Server Password
   Enter the password of the configured "service" user on the camera


   > [!IMPORTANT]
   > This field is created by the `Needs Server` on the Basic tab, do not create this field manually.




   ###### Use SSL
   Select True or False depending on if SSL (HTTPS) should be used to communicate with the camera. This should always be "True"

   ![MobotixIPCamera Custom Field - ServerUseSsl](docsource/images/MobotixIPCamera-custom-field-ServerUseSsl-dialog.png)
   ![MobotixIPCamera Custom Field - ServerUseSsl](docsource/images/MobotixIPCamera-custom-field-ServerUseSsl-validation-options-dialog.png)





   </details>

## Installation

1. **Download the latest Mobotix IP Camera Universal Orchestrator extension from GitHub.**

    Navigate to the [Mobotix IP Camera Universal Orchestrator extension GitHub version page](https://github.com/Keyfactor/mobotix-ipcamera-orchestrator/releases/latest). Refer to the compatibility matrix below to determine the asset should be downloaded. Then, click the corresponding asset to download the zip archive.

   | Universal Orchestrator Version | Latest .NET version installed on the Universal Orchestrator server | `rollForward` condition in `Orchestrator.runtimeconfig.json` | `mobotix-ipcamera-orchestrator` .NET version to download |
   | --------- | ----------- | ----------- | ----------- |
   | Between `11.0.0` and `11.5.1` (inclusive) | `net8.0` | `LatestMajor` | `net8.0` |
   | `11.6` _and_ newer | `net8.0` | | `net8.0` | 

    Unzip the archive containing extension assemblies to a known location.

    > **Note** If you don't see an asset with a corresponding .NET version, you should always assume that it was compiled for `net8.0`.

2. **Locate the Universal Orchestrator extensions directory.**

    * **Default on Windows** - `C:\Program Files\Keyfactor\Keyfactor Orchestrator\extensions`
    * **Default on Linux** - `/opt/keyfactor/orchestrator/extensions`

3. **Create a new directory for the Mobotix IP Camera Universal Orchestrator extension inside the extensions directory.**

    Create a new directory called `mobotix-ipcamera-orchestrator`.
    > The directory name does not need to match any names used elsewhere; it just has to be unique within the extensions directory.

4. **Copy the contents of the downloaded and unzipped assemblies from __step 2__ to the `mobotix-ipcamera-orchestrator` directory.**

5. **Restart the Universal Orchestrator service.**

    Refer to [Starting/Restarting the Universal Orchestrator service](https://software.keyfactor.com/Core-OnPrem/Current/Content/InstallingAgents/NetCoreOrchestrator/StarttheService.htm).


6. **(optional) PAM Integration**

    The Mobotix IP Camera Universal Orchestrator extension is compatible with all supported Keyfactor PAM extensions to resolve PAM-eligible secrets. PAM extensions running on Universal Orchestrators enable secure retrieval of secrets from a connected PAM provider.

    To configure a PAM provider, [reference the Keyfactor Integration Catalog](https://keyfactor.github.io/integrations-catalog/content/pam) to select an extension and follow the associated instructions to install it on the Universal Orchestrator (remote).


> The above installation steps can be supplemented by the [official Command documentation](https://software.keyfactor.com/Core-OnPrem/Current/Content/InstallingAgents/NetCoreOrchestrator/CustomExtensions.htm?Highlight=extensions).



## Defining Certificate Stores



### Store Creation

#### Manually with the Command UI

<details><summary>Click to expand details</summary>

1. **Navigate to the _Certificate Stores_ page in Keyfactor Command.**

    Log into Keyfactor Command, toggle the _Locations_ dropdown, and click _Certificate Stores_.

2. **Add a Certificate Store.**

    Click the Add button to add a new Certificate Store. Use the table below to populate the **Attributes** in the **Add** form.

   | Attribute | Description                                             |
   | --------- |---------------------------------------------------------|
   | Category | Select "Mobotix IP Camera" or the customized certificate store name from the previous step. |
   | Container | Optional container to associate certificate store with. |
   | Client Machine | The IP address of the Camera. Sample is "192.167.231.174:44444". Include the port if necessary. |
   | Store Path | Enter the Serial Number of the camera e.g. `0b7c3d2f9e8a` |
   | Orchestrator | Select an approved orchestrator capable of managing `MobotixIPCamera` certificates. Specifically, one with the `MobotixIPCamera` capability. |
   | ServerUsername | Enter the username of the configured "service" user on the camera |
   | ServerPassword | Enter the password of the configured "service" user on the camera |
   | ServerUseSsl | Select True or False depending on if SSL (HTTPS) should be used to communicate with the camera. This should always be "True" |

</details>



#### Using kfutil CLI

<details><summary>Click to expand details</summary>

1. **Generate a CSV template for the MobotixIPCamera certificate store**

    ```shell
    kfutil stores import generate-template --store-type-name MobotixIPCamera --outpath MobotixIPCamera.csv
    ```
2. **Populate the generated CSV file**

    Open the CSV file, and reference the table below to populate parameters for each **Attribute**.

   | Attribute | Description |
   | --------- | ----------- |
   | Category | Select "Mobotix IP Camera" or the customized certificate store name from the previous step. |
   | Container | Optional container to associate certificate store with. |
   | Client Machine | The IP address of the Camera. Sample is "192.167.231.174:44444". Include the port if necessary. |
   | Store Path | Enter the Serial Number of the camera e.g. `0b7c3d2f9e8a` |
   | Orchestrator | Select an approved orchestrator capable of managing `MobotixIPCamera` certificates. Specifically, one with the `MobotixIPCamera` capability. |
   | Properties.ServerUsername | Enter the username of the configured "service" user on the camera |
   | Properties.ServerPassword | Enter the password of the configured "service" user on the camera |
   | Properties.ServerUseSsl | Select True or False depending on if SSL (HTTPS) should be used to communicate with the camera. This should always be "True" |

3. **Import the CSV file to create the certificate stores**

    ```shell
    kfutil stores import csv --store-type-name MobotixIPCamera --file MobotixIPCamera.csv
    ```

</details>


#### PAM Provider Eligible Fields
<details><summary>Attributes eligible for retrieval by a PAM Provider on the Universal Orchestrator</summary>

If a PAM provider was installed _on the Universal Orchestrator_ in the [Installation](#Installation) section, the following parameters can be configured for retrieval _on the Universal Orchestrator_.

   | Attribute | Description |
   | --------- | ----------- |
   | ServerUsername | Enter the username of the configured "service" user on the camera |
   | ServerPassword | Enter the password of the configured "service" user on the camera |

Please refer to the **Universal Orchestrator (remote)** usage section ([PAM providers on the Keyfactor Integration Catalog](https://keyfactor.github.io/integrations-catalog/content/pam)) for your selected PAM provider for instructions on how to load attributes orchestrator-side.
> Any secret can be rendered by a PAM provider _installed on the Keyfactor Command server_. The above parameters are specific to attributes that can be fetched by an installed PAM provider running on the Universal Orchestrator server itself.

</details>


> The content in this section can be supplemented by the [official Command documentation](https://software.keyfactor.com/Core-OnPrem/Current/Content/ReferenceGuide/Certificate%20Stores.htm?Highlight=certificate%20store).




## Extension Mechanics

At present, the secret fields need to be set to some value due to existing bugs in command. 
The username/password/secret fields need to be set to any value other than empty for jobs to be processed correctly.
"test" is a good value to use for these fields.

## Test Cases

The SOS solution will eventually include a functional test framework. The existing sample framework currently included in the project is out of date and non functional.

## Installation

The compiled binaries for the Orchestrator Extension itself can be deployed to the extensions folder at the location of the Universal Orchestrator installation. 

However, to get the most use out of this extension, it is recommended to use the Visual Studio project. You should either install Visual Studio on the machine you run the orchestrator or link the debugger remotely. 

In case you setup Visual Studio locally, you could use a symlink to link the Visual studio output directory to the extensions folder, specifically making a subfolder named "SOS". 
Once this is done and the code is compiled, you can attach the Visual Studio debugger to the Universal Orchestrator process for efficient debugging and variable inspection. 

The Sample Key Store certificate store type also needs to be added to Keyfactor. The exact settings are available in the install folder in this repository. This extension is configured to automatically log all incoming data it receives from the Universal Orchestrator. The log level needs to be set to at least Debug in the Universal Orchestrator nlog settings for this information to appear in the logs. 

The overview of the Sample Orchestrator Store type is available here:
* [Sample Orchestrator Store Type](docs/sos.md)


## License

Apache License 2.0, see [LICENSE](LICENSE).

## Related Integrations

See all [Keyfactor Universal Orchestrator extensions](https://github.com/orgs/Keyfactor/repositories?q=orchestrator).