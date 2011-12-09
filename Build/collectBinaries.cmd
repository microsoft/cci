
if exist ..\Binaries ( rd /S /Q ..\Binaries )
md ..\Binaries

copy ..\Sources\AstsProjectedAsCodeModel\bin\Release\*.dll ..\Binaries
copy ..\Sources\AstsProjectedAsCodeModel\bin\Release\*.pdb ..\Binaries
copy ..\Sources\AstsProjectedAsCodeModel\bin\Release\*.xml ..\Binaries
copy ..\Metadata\Sources\PeReader\bin\Release\Microsoft.Cci.PeReader.dll ..\Binaries
copy ..\Metadata\Sources\PeReader\bin\Release\Microsoft.Cci.PeReader.pdb ..\Binaries
copy ..\Metadata\Sources\PeReader\bin\Release\Microsoft.Cci.PeReader.xml ..\Binaries
	