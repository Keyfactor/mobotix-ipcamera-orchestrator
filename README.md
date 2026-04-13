<h1 align="center" style="border-bottom: none">
    Sample Universal Orchestrator Extension
</h1>

<p align="center">
  <!-- Badges -->
<img src="https://img.shields.io/badge/integration_status-pilot-3D1973?style=flat-square" alt="Integration Status: pilot" />
<a href="https://github.com/Keyfactor/sample-orchestrator-extension/releases"><img src="https://img.shields.io/github/v/release/Keyfactor/sample-orchestrator-extension?style=flat-square" alt="Release" /></a>
<img src="https://img.shields.io/github/issues/Keyfactor/sample-orchestrator-extension?style=flat-square" alt="Issues" />
<img src="https://img.shields.io/github/downloads/Keyfactor/sample-orchestrator-extension/total?style=flat-square&label=downloads&color=28B905" alt="GitHub Downloads (all assets, all releases)" />
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

This is a Sample Orchestrator Extension with an implemented Sample Orchestrator Store. 
The main purpose is to provide an easy to install and use orchestrator extension for educational and testing purposes, as well as to provide a reference for development and an assembly of best practices. The current version supports discovery, inventory, management and reenrollment for the custom cert store store type, and includes a basic test framework.

The Sample Orchestrator Store functions using the Sample Orchestrator Store API (SOSApi). This system hosts a basic server that provides an API as well as a web app that allows you to manage this store.

The guidelines on SOSApi use are available at [SOSApi Guide](docs/SOSApi.md).



## Compatibility

This integration is compatible with Keyfactor Universal Orchestrator version 25.4 and later.

## Support
The Sample Universal Orchestrator extension is open source and there is **no SLA**. Keyfactor will address issues and feature requests as resources become available. Keyfactor customers may request escalation by opening up a support ticket through their Keyfactor representative.

> To report a problem or suggest a new feature, use the **[Issues](../../issues)** tab. If you want to contribute bug fixes or additional enhancements, use the **[Pull requests](../../pulls)** tab.

## Requirements & Prerequisites

Before installing the Sample Universal Orchestrator extension, we recommend that you install [kfutil](https://github.com/Keyfactor/kfutil). Kfutil is a command-line tool that simplifies the process of creating store types, installing extensions, and instantiating certificate stores in Keyfactor Command.



## SOS Certificate Store Type

To use the Sample Universal Orchestrator extension, you **must** create the SOS Certificate Store Type. This only needs to happen _once_ per Keyfactor Command instance.



The Sample Orchestrator Store utilizes the SOSApi included in this solution. To utilize SOSApi and download the solution. 
Build the SOSApi project and use the compiled binary to host the API/Web interface.




#### Sample Orchestrator Solution Requirements

The Orchestrator needs to be able to access the IP SOSApi is running under (0.0.0.0/8080 by default).



#### Supported Operations

| Operation    | Is Supported                                                                                                           |
|--------------|------------------------------------------------------------------------------------------------------------------------|
| Add          | ✅ Checked        |
| Remove       | ✅ Checked     |
| Discovery    | ✅ Checked  |
| Reenrollment | ✅ Checked |
| Create       | ✅ Checked     |

#### Store Type Creation

##### Using kfutil:
`kfutil` is a custom CLI for the Keyfactor Command API and can be used to create certificate store types.
For more information on [kfutil](https://github.com/Keyfactor/kfutil) check out the [docs](https://github.com/Keyfactor/kfutil?tab=readme-ov-file#quickstart)
   <details><summary>Click to expand SOS kfutil details</summary>

   ##### Using online definition from GitHub:
   This will reach out to GitHub and pull the latest store-type definition
   ```shell
   # Sample Orchestrator Solution
   kfutil store-types create SOS
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
Below are instructions on how to create the SOS store type manually in
the Keyfactor Command Portal
   <details><summary>Click to expand manual SOS details</summary>

   Create a store type called `SOS` with the attributes in the tables below:

   ##### Basic Tab
   | Attribute | Value | Description |
   | --------- | ----- | ----- |
   | Name | Sample Orchestrator Solution | Display name for the store type (may be customized) |
   | Short Name | SOS | Short display name for the store type |
   | Capability | SOS | Store type name orchestrator will register with. Check the box to allow entry of value |
   | Supports Add | ✅ Checked | Check the box. Indicates that the Store Type supports Management Add |
   | Supports Remove | ✅ Checked | Check the box. Indicates that the Store Type supports Management Remove |
   | Supports Discovery | ✅ Checked | Check the box. Indicates that the Store Type supports Discovery |
   | Supports Reenrollment | ✅ Checked |  Indicates that the Store Type supports Reenrollment |
   | Supports Create | ✅ Checked | Check the box. Indicates that the Store Type supports store creation |
   | Needs Server | ✅ Checked | Determines if a target server name is required when creating store |
   | Blueprint Allowed | ✅ Checked | Determines if store type may be included in an Orchestrator blueprint |
   | Uses PowerShell | 🔲 Unchecked | Determines if underlying implementation is PowerShell |
   | Requires Store Password | ✅ Checked | Enables users to optionally specify a store password when defining a Certificate Store. |
   | Supports Entry Password | ✅ Checked | Determines if an individual entry within a store can have a password. |

   The Basic tab should look like this:

   ![SOS Basic Tab](docsource/images/SOS-basic-store-type-dialog.png)

   ##### Advanced Tab
   | Attribute | Value | Description |
   | --------- | ----- | ----- |
   | Supports Custom Alias | Forbidden | Determines if an individual entry within a store can have a custom Alias. |
   | Private Key Handling | Optional | This determines if Keyfactor can send the private key associated with a certificate to the store. Required because IIS certificates without private keys would be invalid. |
   | PFX Password Style | Default | 'Default' - PFX password is randomly generated, 'Custom' - PFX password may be specified when the enrollment job is created (Requires the Allow Custom Password application setting to be enabled.) |

   The Advanced tab should look like this:

   ![SOS Advanced Tab](docsource/images/SOS-advanced-store-type-dialog.png)

   > For Keyfactor **Command versions 24.4 and later**, a Certificate Format dropdown is available with PFX and PEM options. Ensure that **PFX** is selected, as this determines the format of new and renewed certificates sent to the Orchestrator during a Management job. Currently, all Keyfactor-supported Orchestrator extensions support only PFX.

   ##### Custom Fields Tab
   Custom fields operate at the certificate store level and are used to control how the orchestrator connects to the remote target server containing the certificate store to be managed. The following custom fields should be added to the store type:

   | Name | Display Name | Description | Type | Default Value/Options | Required |
   | ---- | ------------ | ---- | --------------------- | -------- | ----------- |
   | StoreNameString | Store Name | The Store name for the particular SOS store. | String |  | 🔲 Unchecked |
   | ForTestingOnlyBool | For Testing Only | Test bool variable. | Bool | true | 🔲 Unchecked |
   | CollectionNameMultipleChoice | Collection Name | A test collection. | MultipleChoice | internal | ✅ Checked |
   | PrivateDetailsSecret | Private Details | A test secret. | Secret | test | 🔲 Unchecked |

   The Custom Fields tab should look like this:

   ![SOS Custom Fields Tab](docsource/images/SOS-custom-fields-store-type-dialog.png)


   ###### Store Name
   The Store name for the particular SOS store.

   ![SOS Custom Field - StoreNameString](docsource/images/SOS-custom-field-StoreNameString-dialog.png)



   ###### For Testing Only
   Test bool variable.

   ![SOS Custom Field - ForTestingOnlyBool](docsource/images/SOS-custom-field-ForTestingOnlyBool-dialog.png)



   ###### Collection Name
   A test collection.

   ![SOS Custom Field - CollectionNameMultipleChoice](docsource/images/SOS-custom-field-CollectionNameMultipleChoice-dialog.png)



   ###### Private Details
   A test secret.

   ![SOS Custom Field - PrivateDetailsSecret](docsource/images/SOS-custom-field-PrivateDetailsSecret-dialog.png)





   ##### Entry Parameters Tab

   | Name | Display Name | Description | Type | Default Value | Entry has a private key | Adding an entry | Removing an entry | Reenrolling an entry |
   | ---- | ------------ | ---- | ------------- | ----------------------- | ---------------- | ----------------- | ------------------- | ----------- |
   | CommaSeparatedSansString | SANs | SAN string. | String |  | 🔲 Unchecked | 🔲 Unchecked | 🔲 Unchecked | ✅ Checked |
   | CertColorMultipleChoice | Certificate Color | A test variable with multiple choice. | MultipleChoice | red | 🔲 Unchecked | 🔲 Unchecked | 🔲 Unchecked | 🔲 Unchecked |
   | ForTestingOnlyBool | For Testing Only | Another test boolean. | Bool | true | ✅ Checked | 🔲 Unchecked | 🔲 Unchecked | 🔲 Unchecked |
   | PrivateCertDetailsSecret | Private Cert Details | A per cert secret. | Secret | test | 🔲 Unchecked | 🔲 Unchecked | 🔲 Unchecked | 🔲 Unchecked |

   The Entry Parameters tab should look like this:

   ![SOS Entry Parameters Tab](docsource/images/SOS-entry-parameters-store-type-dialog.png)


   ##### SANs
   SAN string.

   ![SOS Entry Parameter - CommaSeparatedSansString](docsource/images/SOS-entry-parameters-store-type-dialog-CommaSeparatedSansString.png)


   ##### Certificate Color
   A test variable with multiple choice.

   ![SOS Entry Parameter - CertColorMultipleChoice](docsource/images/SOS-entry-parameters-store-type-dialog-CertColorMultipleChoice.png)


   ##### For Testing Only
   Another test boolean.

   ![SOS Entry Parameter - ForTestingOnlyBool](docsource/images/SOS-entry-parameters-store-type-dialog-ForTestingOnlyBool.png)


   ##### Private Cert Details
   A per cert secret.

   ![SOS Entry Parameter - PrivateCertDetailsSecret](docsource/images/SOS-entry-parameters-store-type-dialog-PrivateCertDetailsSecret.png)



   </details>

## Installation

1. **Download the latest Sample Universal Orchestrator extension from GitHub.**

    Navigate to the [Sample Universal Orchestrator extension GitHub version page](https://github.com/Keyfactor/sample-orchestrator-extension/releases/latest). Refer to the compatibility matrix below to determine the asset should be downloaded. Then, click the corresponding asset to download the zip archive.

   | Universal Orchestrator Version | Latest .NET version installed on the Universal Orchestrator server | `rollForward` condition in `Orchestrator.runtimeconfig.json` | `sample-orchestrator-extension` .NET version to download |
   | --------- | ----------- | ----------- | ----------- |
   | Between `11.0.0` and `11.5.1` (inclusive) | `net8.0` | `LatestMajor` | `net8.0` |
   | `11.6` _and_ newer | `net8.0` | | `net8.0` | 

    Unzip the archive containing extension assemblies to a known location.

    > **Note** If you don't see an asset with a corresponding .NET version, you should always assume that it was compiled for `net8.0`.

2. **Locate the Universal Orchestrator extensions directory.**

    * **Default on Windows** - `C:\Program Files\Keyfactor\Keyfactor Orchestrator\extensions`
    * **Default on Linux** - `/opt/keyfactor/orchestrator/extensions`

3. **Create a new directory for the Sample Universal Orchestrator extension inside the extensions directory.**

    Create a new directory called `sample-orchestrator-extension`.
    > The directory name does not need to match any names used elsewhere; it just has to be unique within the extensions directory.

4. **Copy the contents of the downloaded and unzipped assemblies from __step 2__ to the `sample-orchestrator-extension` directory.**

5. **Restart the Universal Orchestrator service.**

    Refer to [Starting/Restarting the Universal Orchestrator service](https://software.keyfactor.com/Core-OnPrem/Current/Content/InstallingAgents/NetCoreOrchestrator/StarttheService.htm).


6. **(optional) PAM Integration**

    The Sample Universal Orchestrator extension is compatible with all supported Keyfactor PAM extensions to resolve PAM-eligible secrets. PAM extensions running on Universal Orchestrators enable secure retrieval of secrets from a connected PAM provider.

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
   | Category | Select "Sample Orchestrator Solution" or the customized certificate store name from the previous step. |
   | Container | Optional container to associate certificate store with. |
   | Client Machine | The base URL of the SOS API (i.e. http://localhost:8080) |
   | Store Path | The name of the store as defined in the SOS system (i.e. SampleKeyStore2). |
   | Store Password | Password to use when reading/writing to store |
   | Orchestrator | Select an approved orchestrator capable of managing `SOS` certificates. Specifically, one with the `SOS` capability. |
   | StoreNameString | The Store name for the particular SOS store. |
   | ForTestingOnlyBool | Test bool variable. |
   | CollectionNameMultipleChoice | A test collection. |
   | PrivateDetailsSecret | A test secret. |

</details>



#### Using kfutil CLI

<details><summary>Click to expand details</summary>

1. **Generate a CSV template for the SOS certificate store**

    ```shell
    kfutil stores import generate-template --store-type-name SOS --outpath SOS.csv
    ```
2. **Populate the generated CSV file**

    Open the CSV file, and reference the table below to populate parameters for each **Attribute**.

   | Attribute | Description |
   | --------- | ----------- |
   | Category | Select "Sample Orchestrator Solution" or the customized certificate store name from the previous step. |
   | Container | Optional container to associate certificate store with. |
   | Client Machine | The base URL of the SOS API (i.e. http://localhost:8080) |
   | Store Path | The name of the store as defined in the SOS system (i.e. SampleKeyStore2). |
   | Store Password | Password to use when reading/writing to store |
   | Orchestrator | Select an approved orchestrator capable of managing `SOS` certificates. Specifically, one with the `SOS` capability. |
   | Properties.StoreNameString | The Store name for the particular SOS store. |
   | Properties.ForTestingOnlyBool | Test bool variable. |
   | Properties.CollectionNameMultipleChoice | A test collection. |
   | Properties.PrivateDetailsSecret | A test secret. |

3. **Import the CSV file to create the certificate stores**

    ```shell
    kfutil stores import csv --store-type-name SOS --file SOS.csv
    ```

</details>


#### PAM Provider Eligible Fields
<details><summary>Attributes eligible for retrieval by a PAM Provider on the Universal Orchestrator</summary>

If a PAM provider was installed _on the Universal Orchestrator_ in the [Installation](#Installation) section, the following parameters can be configured for retrieval _on the Universal Orchestrator_.

   | Attribute | Description |
   | --------- | ----------- |
   | ServerUsername | Username to use when connecting to server |
   | ServerPassword | Password to use when connecting to server |
   | StorePassword | Password to use when reading/writing to store |
   | PrivateDetailsSecret | A test secret. |

Please refer to the **Universal Orchestrator (remote)** usage section ([PAM providers on the Keyfactor Integration Catalog](https://keyfactor.github.io/integrations-catalog/content/pam)) for your selected PAM provider for instructions on how to load attributes orchestrator-side.
> Any secret can be rendered by a PAM provider _installed on the Keyfactor Command server_. The above parameters are specific to attributes that can be fetched by an installed PAM provider running on the Universal Orchestrator server itself.

</details>


> The content in this section can be supplemented by the [official Command documentation](https://software.keyfactor.com/Core-OnPrem/Current/Content/ReferenceGuide/Certificate%20Stores.htm?Highlight=certificate%20store).


### Extension Mechanics

At present, the secret fields need to be set to some value due to existing bugs in command. 
The username/password/secret fields need to be set to any value other than empty for jobs to be processed correctly.
"test" is a good value to use for these fields.

### Test Cases

The SOS solution will eventually include a functional test framework. The existing sample framework currently included in the project is out of date and non functional.

### Installation

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