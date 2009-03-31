csc /t:module MRW_Module1.cs
csc /t:module /addmodule:MRW_Module1.netmodule MRW_Module2.cs
csc /unsafe /t:library /addmodule:MRW_Module1.netmodule /addmodule:MRW_Module2.netmodule /keyfile:..\..\Common\InterimKey.snk MRW_Assembly.cs
csc /t:library /r:vjslib.dll /r:MRW_Assembly.dll /keyfile:..\..\Common\InterimKey.snk MRW_TestAssembly.cs
cl /clr:pure MRW_CppAssembly.cpp /LD
ilasm /dll MRW_ILAsmAssembly.il