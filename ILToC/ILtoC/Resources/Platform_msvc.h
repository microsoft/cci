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
#include <float.h>
#include <intrin.h>
#include <windows.h>

#ifndef ILTOC_TLS_TYPE
#define ILTOC_TLS_TYPE
typedef uint32_t tls_type;
#endif

void* GetAppDomainStaticBlock();
void AllocateThreadLocal(tls_type * key);
void* GetThreadLocalValue(tls_type key);
void SetThreadLocalValue(tls_type key, void * value);
uint32_t Increment_and_align(uint32_t offset, uint32_t increment);
uint64_t Infinite();
void SetThreadLocals(void* appDomainStatics);

#define isfinite _finite
//TODO: need a specialized implementation for this
#define memequals(s1, s2, n) memcmp(s1, s2, n) == 0
//TODO: need a specialized implementation for memcmp. The standard msvc runtime function may not be up to scratch.

void CreateNewThread(void** handle, uint32_t stackSize, void* startAddress, void* parameter, uint32_t *threadId);