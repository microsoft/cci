setlocal
echo build [version number] [codeplex user] [codeplex password]
set MSBuildArguments=ccisharp.build
if NOT "%1" == "" set MSBuildArguments=%MSBuildArguments% "/p:CCNetLabel=%1"
if NOT "%2" == "" set MSBuildArguments=%MSBuildArguments% "/p:CodePlexUser=%2"
if NOT "%3" == "" set MSBuildArguments=%MSBuildArguments% "/p:CodePlexPassword=%3"
"%windir%\Microsoft.NET\Framework\v3.5\MSBuild.exe" %MSBuildArguments%
