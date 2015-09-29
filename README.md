# Grandstream-Provisioning-Tool
A GUI utility for creating configuration files for Grandstream devices
## About
Grandstream Analog Telephone Adapters (ATAs) and other devices support remote configuration provisioning in the form of retrieving an (un)encrypted configuration binary created from configuration templates available on Grandstream's website.

![Grandstream Provisioning Tool GUI](http://i.imgur.com/6gscWPX.png)

## Features
* GUI support for up to 8 FXS ports
* GUI support for up to 2 SIP Profiles
* Ability to load configuration templates provided on Grandstream's website.
* Ability to concatenate or remove custom configuration values on top of a loaded template
* XML configuration format
* XML configuration encryption
* Legacy configuration format
* Legacy configuration encryption*

## License Info
* Released under the Apache 2.0 license. See the LICENSE file for more information.
* This utility is not developed or supported by Grandstream Networks, Inc.
* AES-256-CBC encryption is provided by [AESCryptNET](https://github.com/SH4ZB0T/AESCryptNET) under the LGPLv3 License.
* Grandstream's .NET legacy encoding and encryption API (gs_config.dll) is permitted for public redistribution and usage under no support nor warranty.

*Grandstream does not recommend using legacy encryption because it is deemed insecure. Configurataion files containing sensitive information should use XML format and a provided encryption password instead.