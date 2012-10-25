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
#include <wchar.h>
#include <math.h>
#include <stdarg.h>

#ifndef ILTOC_TLS_TYPE
#define ILTOC_TLS_TYPE
typedef pthread_key_t tls_type;
#endif

void* GetAppDomainStaticBlock();
void AllocateThreadLocal(tls_type * key);
void* GetThreadLocalValue(tls_type key);
void SetThreadLocalValue(tls_type key, void * value);
uint32_t Increment_and_align(uint32_t offset, uint32_t increment);
void SetThreadLocals(void* appDomainStatics);

//#define isfinite _finite
//TODO: need a specialized implementation for this
#define memequals(s1, s2, n) memcmp(s1, s2, n) == 0
//TODO: need a specialized implementation for memcmp. The standard msvc runtime function may not be up to scratch.

void GetSystemTimeAsFileTime(int64_t * systemTimeAsFileTime);
uint32_t GetTickCount();
uint64_t Infinite();
uint32_t _InterlockedCompareExchange(uint32_t* destination, uint32_t exchange, uint32_t comparand);
uint64_t _InterlockedCompareExchange64(uint64_t* destination, uint64_t exchange, uint64_t comparand);
uint32_t _InterlockedExchange(uint32_t* destination, uint32_t exchange);
void _mm_pause();
void CreateNewThread(void** handle, uint32_t stackSize, void* startAddress, void* parameter, uint32_t *threadId);
uintptr_t GetCurrentThread();
void MemoryBarrier();
int ResumeThread(void* thread);
uint32_t TerminateThread(void* thread, uint32_t exitCode);
uint32_t CloseHandle(void* handle);
int32_t WaitForSingleObject(void* handle, uint64_t milliseconds);
void Sleep(int32_t milliseconds);