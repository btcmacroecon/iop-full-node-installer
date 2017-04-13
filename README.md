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


# Limitations, Known Issues, Troubleshooting

The server installer does not check that every operation completes successfully or that all user inputs are actually valid. For example when the server installer 
runs the servers for the first time, it does not check whether they start start successfullly and work as expected. Another example is when the server installer 
installs scheduled tasks on Windows, it asks for a password and does not verify if the entered password is correct.

Things can go wrong especially during the first start of the servers (it is known that CAN server sometimes fails for some reason when started for the first time). 
This behavior can be improved in the future, but until then, you can do the following:

 * there should be 4 server processes running once the installer is complete - IoPd, iop-locnet, ipfs, and ProfileServer,
 * if not, you can try to start the servers that are missing - use Task Scheduler on Windows and init.d scripts on Linux, or simply reboot the machine,
 * it is always safer to use default values in the installer,
 * except for the CAN server, all components create logs in their application data folders, you can inspect the logs to find the problem.

If the installation fails, please find the log file in Logs directory that will be created in the `installer` folder. Analyzing the log will help you identify and understand the problem.