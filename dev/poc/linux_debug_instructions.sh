__doc__="
References:
    https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu-install?tabs=dotnet9&pivots=os-linux-ubuntu-2404
"

# On 22.04
export DEBIAN_FRONTEND=noninteractive
sudo add-apt-repository -y ppa:dotnet/backports
sudo apt-get update
sudo apt-get install -y dotnet-sdk-9.0 dotnet-runtime-9.0


sudo apt install android-sdk

