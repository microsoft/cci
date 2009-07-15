setlocal
rem codeplex updateload script:
rem usage:
rem  createrelease <clean depot location> <version number> <codeplex user name> <codeplex password>

%windir%\microsoft.net\framework\v3.5\msbuild.exe cciast.build /t:CreateCodePlexRelease /p:CleanRootDirectory=%1 /p:CCNetLabel=%2 /p:CodeplexUser=%3 /p:CodeplexPassword=%4
