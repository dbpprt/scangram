rm -r scangram
git clone https://github.com/dennisbappert/scangram
cd ./scangram

dotnet restore
dotnet build
dotnet publish --configuration Release --output ./dist

cd ./src/dist

export LD_LIBRARY_PATH=$LD_LIBRARY_PATH:/usr/local/lib
dotnet Scangram.dll