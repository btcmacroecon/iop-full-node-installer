#!/bin/bash

if [[ $EUID -ne 0 ]]; then
   echo "This script should be run under sudo." 
   exit 1
fi

echo "Installing .NET Core dependencies."
apt-get update
apt-get install -qy libunwind8 libcurl4-openssl-dev 

BASEDIR=$(dirname "$0")
cd "$BASEDIR/installer"

chmod +x ./FullNodeInstaller
echo "-----------------------------------"
./FullNodeInstaller 
