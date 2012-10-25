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
#include <stdlib.h> 
#include <stdint.h>
#include <pthread.h>
#include "Platform_unix.h"

extern tls_type appdomain_static_block_tlsIndex;
extern tls_type thread_static_block_tlsIndex;
extern uint32_t thread_static_block_size;

void* GetAppDomainStaticBlock() {
  return pthread_getspecific(appdomain_static_block_tlsIndex);
}

void AllocateThreadLocal(tls_type * key) {
	pthread_key_create(key, NULL);
}

void* GetThreadLocalValue(tls_type key) {
  return pthread_getspecific(key);
}

void SetThreadLocalValue(tls_type key, void * value) {
  pthread_setspecific(key, value);
}

void SetThreadLocals(void* appDomainStatics) {
  void* threadStatics = calloc(1, thread_static_block_size);
  pthread_setspecific(thread_static_block_tlsIndex, threadStatics);
  pthread_setspecific(appdomain_static_block_tlsIndex, appDomainStatics);
}

void GetSystemTimeAsFileTime(int64_t * systemTimeAsFileTime) {
	// TODO
}

uint32_t GetTickCount(){
	// TODO
	return 0;
}

uint64_t Infinite() {
	// TODO
  return 0;
}

uint32_t _InterlockedCompareExchange(uint32_t* destination, uint32_t exchange, uint32_t comparand) {
	// TODO
	*destination = exchange;
	return comparand;
}

uint64_t _InterlockedCompareExchange64(uint64_t* destination, uint64_t exchange, uint64_t comparand) {
	// TODO
	*destination = exchange;
	return comparand;
}

uint32_t _InterlockedExchange(uint32_t* destination, uint32_t exchange) {
	// TODO
	uint32_t initial = * destination;
	*destination = exchange;
	return initial;
}

void _mm_pause() {
	//TODO
}

void CreateNewThread(void** handle, uint32_t stackSize, void* startAddress, void* parameter, uint32_t *threadId) {
	int32_t ret = 0;
	pthread_attr_t attr;
	pthread_t t;
	ret = pthread_attr_init(&attr);
	if (ret != 0) {
		*handle = NULL;
		return;
	}
	ret = pthread_attr_setstacksize(&attr, stackSize);
	if (ret != 0) {
		*handle = NULL;
		return;
	}
	ret = pthread_create(&t, &attr, startAddress, parameter);
	if (ret != 0) {
		*handle = NULL;
		return;
	}
	*handle = &t;
	*threadId = (uint32_t)t;
}

uintptr_t GetCurrentThread() {
	return pthread_self();
}

void MemoryBarrier() {
	//TODO
}

int ResumeThread(void* thread) {
	//TODO
	return 0;
}
uint32_t TerminateThread(void* thread, uint32_t exitCode) {
	//TODO
	return 1;
}

uint32_t CloseHandle(void* handle) {
	//TODO
	return 1;
}

int32_t WaitForSingleObject(void* handle, uint64_t milliseconds) {
	//TODO
	return 0;
}

void Sleep(int32_t milliseconds) {
	//TODO
}