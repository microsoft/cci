
if exist ..\Binaries ( rd /S /Q ..\Binaries )
md ..\Binaries

copy ..\Sources\AstsProjectedAsCodeModel\bin\Debug\*.dll ..\Binaries
copy ..\Sources\AstsProjectedAsCodeModel\bin\Debug\*.pdb ..\Binaries
copy ..\Sources\AstsProjectedAsCodeModel\bin\Debug\*.xml ..\Binaries
copy ..\Metadata\Sources\PeReader\bin\Debug\Microsoft.Cci.PeReader.dll ..\Binaries
copy ..\Metadata\Sources\PeReader\bin\Debug\Microsoft.Cci.PeReader.pdb ..\Binaries
copy ..\Metadata\Sources\PeReader\bin\Debug\Microsoft.Cci.PeReader.xml ..\Binaries
	