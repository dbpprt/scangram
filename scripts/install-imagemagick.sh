
sudo apt-get -y update
sudo apt-get -y install libpng12-dev libglib2.0-dev zlib1g-dev libbz2-dev libtiff5-dev libjpeg8-dev

wget http://www.imagemagick.org/download/ImageMagick.tar.gz
tar -xvf ImageMagick.tar.gz
cd ImageMagick-7.*

./configure
make
sudo make install
sudo ldconfig /usr/local/lib