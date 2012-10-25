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
#include <windows.h>
#include <stdlib.h>
#include <stdint.h>
#include "Platform_msvc.h"

extern tls_type appdomain_static_block_tlsIndex;
extern tls_type thread_static_block_tlsIndex;
extern uint32_t thread_static_block_size;

void* GetAppDomainStaticBlock() {
  return TlsGetValue(appdomain_static_block_tlsIndex);
}

void AllocateThreadLocal(tls_type * key) {
	*key = TlsAlloc();
}

void* GetThreadLocalValue(tls_type key) {
  return TlsGetValue(key);
}

void SetThreadLocalValue(tls_type key, void * value) {
  TlsSetValue(key, value);
}

uint64_t Infinite() {
  return INFINITE;
}

void SetThreadLocals(void* appDomainStatics) {
  void* threadStatics = calloc(1, thread_static_block_size);
  TlsSetValue(thread_static_block_tlsIndex, threadStatics);
  TlsSetValue(appdomain_static_block_tlsIndex, appDomainStatics);
}

void CreateNewThread(void** handle, uint32_t stackSize, void* startAddress, void* parameter, uint32_t *threadId) {
	*handle = CreateThread(NULL, stackSize, startAddress, parameter, 0, threadId);
}



