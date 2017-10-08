# 1. KEEP UBUNTU OR DEBIAN UP TO DATE

sudo apt-get -y update

# 2. INSTALL THE DEPENDENCIES

# Build tools:
sudo apt-get install -y apt-transport-https

# install DotNetCore 2
curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg
mv microsoft.gpg /etc/apt/trusted.gpg.d/microsoft.gpg
sh -c 'echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-ubuntu-xenial-prod xenial main" > /etc/apt/sources.list.d/dotnetdev.list'
apt-get -y update
apt-get -y install dotnet-sdk-2.0.0