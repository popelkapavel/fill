set VER=014
del www\fill%VER%.zip
del www\fill%VER%_src.zip
cd bin
7z a -tzip ..\www\fill%VER%.zip fill.exe ..\fill.rtf
@rem mesh.dll ..\cubes.rtf ..\cubes.ini
cd ..
7z a -tzip www\fill%VER%_src.zip *.* Properties img -xr!.vs

