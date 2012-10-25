// ==++== 
// 
//   
//    Copyright (c) 2012 Microsoft Corporation.  All rights reserved.
//   
//    The use and distribution terms for this software are contained in the file
//    named license.txt, which can be found in the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by the
//    terms of this license.
//   
//    You must not remove this notice, or any other, from this software.
//   
// 
// ==--==

uint32_t Increment_and_align(uint32_t offset, uint32_t increment);

#if defined _WIN32 || defined _WIN64 || defined __WIN32__ || defined __TOS_WIN__ || defined __WINDOWS__
#include "Platform_msvc.h"
#endif

#if defined __unix__ || defined __unix || defined __linux__ || defined linux || defined __linux
#include "Platform_unix.h"
#endif