# 1. KEEP UBUNTU OR DEBIAN UP TO DATE

sudo apt-get -y update

# 2. INSTALL THE DEPENDENCIES

# Build tools:
sudo apt-get install -y build-essential cmake

# Dependencies:
sudo apt-get install -y cmake git libgtk2.0-dev pkg-config libavcodec-dev libavformat-dev libswscale-dev

# 3. INSTALL THE LIBRARY (YOU CAN CHANGE '3.2.0' FOR THE LAST STABLE VERSION)

sudo apt-get install -y unzip wget

wget https://github.com/opencv/opencv/archive/3.2.0.zip
unzip 3.2.0.zip
rm 3.2.0.zip
mv opencv-3.2.0 OpenCV

wget https://github.com/opencv/opencv_contrib/archive/3.2.0.zip
unzip 3.2.0.zip
rm 3.2.0.zip
mv opencv_contrib-3.2.0 OpenCV-Contrib

cd OpenCV
mkdir build
cd build
cmake -D CMAKE_BUILD_TYPE=Release -D OPENCV_EXTRA_MODULES_PATH=./../../OpenCV-Contrib/modules -D CMAKE_INSTALL_PREFIX=/usr/local ..
make -j4
sudo make install
sudo ldconfig