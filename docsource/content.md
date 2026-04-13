## Overview

The Sample Orchestrator Store utilizes the SOSApi included in this solution. To utilize SOSApi and download the solution. 
Build the SOSApi project and use the compiled binary to host the API/Web interface.

## Requirements

The Orchestrator needs to be able to access the IP SOSApi is running under (0.0.0.0/8080 by default).

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
