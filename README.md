# Cloud9 - Virtual File System
Cloud9 is a system for creating running / mounting virtual file systems via Dokan
it supports multiple ways of streaming files
## Features
- #### Streaming files from a device (from different providers)
  - SFTP
  - CLONE (own device)
  - [COMING SOON] ...
- #### Instance configs
  - Reading and running instance without additional data
- #### Customizable API
  - Change the way files are shown
  - Everything is modular, you can create all of the parts yourself and any arrangement will work with another
## Intended usage
You can of course use this program however you want.
The recommended way however is to have a config file for an instance and a script which runs the program using the config file to start the instance. This script can then be added to the autostart and run as long as the device.
## Installation
1. Install Dokan
2. Download Repo
3. Create a config JSON file
4. Run client using "cloud9client client mycfg.json"
5. Enjoy!