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

## Installing *cloud9client*
To install cloud9client download this repo and build it from source.
Releases for cloud9client are *not yet* available and it must be built from source.

## Installing *cloud9service* (via *cloud9installer*)
Download cloud9installer.exe from the latest release or build it from source using `cloud9installer.nsi`, 
then run the installer and restart your computer to be sure. A shortcut to `http://localhost:4994/` should be created, click it and enjoy!

## Installing *cloud9service* (from source)
Download this repo and build *cloud9service* from scratch. Then put it in a directory together with app.html and run it.