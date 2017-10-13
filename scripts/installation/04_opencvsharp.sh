# 1. KEEP UBUNTU OR DEBIAN UP TO DATE

sudo apt-get -y update

git clone https://github.com/shimat/opencvsharp.git

#install the Extern lib.
cd opencvsharp/src
sed -i.bak '5i\
include_directories("/usr/local/include/")\
set (CMAKE_CXX_STANDARD 11)\
' CMakeLists.txt
cmake .
make -j 4
make install