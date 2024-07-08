# Cloud9 - Virtual File System
Cloud9 is a system for creating running / mounting virtual file systems via Dokan
it supports multiple ways of streaming files
## Features
- #### Streaming files from a device (from different providers)
  - SFTP
  - [COMING SOON] SOCK  (socket) 
  - [COMING SOON] CLONE (own device)
  - [COMING SOON] HTTP
  - [COMING SOON] ...
- #### [COMING SOON] Streaming files to another device
  - [COMING SOON] SOCK (socket)
  - [COMING SOON] HTTP
- #### Instance configs
  - Reading and running instance without additional data
  - [COMING SOON] Host configs
  - [COMING SOON] Writing instance config from host config
## Intended usage
You can of course use this program however you want.
The recommended way however is to have a config file for an instance and a script which runs the program using the config file to start the instance. This script can then be added to the autostart and run as long as the device.
## Installation
Nada