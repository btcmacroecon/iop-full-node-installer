# IoP Server Installer

IoP Server Installer is a tool that helps installing the following components of IoP network infrastructure:

 * IoP Core Wallet
 * IoP LOC Server
 * IoP CAN Server
 * IoP Profile Server

In the current version of the installer, the installation process can not be fully automatic. Manual interaction from the user is required especially for opening several firewall ports 
for installed servers. 


# Installation

Download an unpack the latest [server installer binary release](https://github.com/Fermat-ORG/iop-server-installer/releases) for your operating system.
You should find a single script and a single folder inside. All you need to do is run the script and then follow the instructions on the screen.

Windows users need to execute the script with administrator privileges. 

Linux users need to execute the script using `sudo`. Linux users may execute the script when they are logged in as `root`, but they should avoid 
running the script under `sudo su` as this will produce wrong file access rights in many cases.


# Limitations and Future Work

Currently all components are only downloaded, installed and configured. The components are not started automatically by the installer, 
nor they are installed to start after the reboot.
