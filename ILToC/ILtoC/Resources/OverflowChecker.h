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

#define Add_intptr_t_int32_t Add_int32_t_intptr_t
#define Add_uintptr_t_uint32_t Add_uint32_t_uintptr_t

#define Subtract_intptr_t_int32_t Subtract_int32_t_intptr_t
#define Subtract_uintptr_t_uint32_t Subtract_uint32_t_uintptr_t

#define Multiply_intptr_t_int32_t Multiply_int32_t_intptr_t
#define Multiply_uintptr_t_uint32_t Multiply_uint32_t_uintptr_t

int32_t Add_int32_t_int32_t(int32_t lhs, int32_t rhs, int *flag);
int64_t Add_int64_t_int64_t(int64_t lhs, int64_t rhs, int *flag);
intptr_t Add_int32_t_intptr_t(int32_t lhs, intptr_t rhs, int *flag);
intptr_t Add_intptr_t_intptr_t(intptr_t lhs, intptr_t rhs, int *flag);
uint32_t Add_uint32_t_uint32_t(uint32_t lhs, uint32_t rhs, int *flag);
uint64_t Add_uint64_t_uint64_t(uint64_t lhs, uint64_t rhs, int *flag);
uintptr_t Add_uint32_t_uintptr_t(uint32_t lhs, uintptr_t rhs, int *flag);
uintptr_t Add_uintptr_t_uintptr_t(uintptr_t lhs, uintptr_t rhs, int *flag);

int32_t Subtract_int32_t_int32_t(int32_t lhs, int32_t rhs, int *flag);
int64_t Subtract_int64_t_int64_t(int64_t lhs, int64_t rhs, int *flag);
intptr_t Subtract_int32_t_intptr_t(int32_t lhs, intptr_t rhs, int *flag);
intptr_t Subtract_intptr_t_intptr_t(intptr_t lhs, intptr_t rhs, int *flag);
uint32_t Subtract_uint32_t_uint32_t(uint32_t lhs, uint32_t rhs, int *flag);
uint64_t Subtract_uint64_t_uint64_t(uint64_t lhs, uint64_t rhs, int *flag);
uintptr_t Subtract_uint32_t_uintptr_t(uint32_t lhs, uintptr_t rhs, int *flag);
uintptr_t Subtract_uintptr_t_uintptr_t(uintptr_t lhs, uintptr_t rhs, int *flag);

int32_t Multiply_int32_t_int32_t(int32_t a, int32_t b, int *flag);
int64_t Multiply_int64_t_int64_t(int64_t a, int64_t b, int *flag);
intptr_t Multiply_int32_t_intptr_t(int32_t a, intptr_t b, int *flag);
intptr_t Multiply_intptr_t_intptr_t(intptr_t a, intptr_t b, int *flag);
uint64_t Multiply_uint64_t_uint64_t(uint64_t a, uint64_t b, int *flag);
uint32_t Multiply_uint32_t_uint32_t(uint32_t a, uint32_t b, int *flag);
uintptr_t Multiply_uint32_t_uintptr_t(uint32_t a, uintptr_t b, int *flag);
uintptr_t Multiply_uintptr_t_uintptr_t(uintptr_t a, uintptr_t b, int *flag);

int8_t Convert_int16_t_to_int8_t(int16_t a, int *flag);
int8_t Convert_int32_t_to_int8_t(int32_t a, int *flag);
int8_t Convert_int64_t_to_int8_t(int64_t a, int *flag);
int8_t Convert_intptr_t_to_int8_t(intptr_t a, int *flag);
int16_t Convert_int32_t_to_int16_t(int32_t a, int *flag);
int16_t Convert_int64_t_to_int16_t(int64_t a, int *flag);
int16_t Convert_intptr_t_to_int16_t(intptr_t a, int *flag);
int32_t Convert_int64_t_to_int32_t(int64_t a, int *flag);
int32_t Convert_intptr_t_to_int32_t(intptr_t a, int *flag);
int64_t Convert_intptr_t_to_int64_t(intptr_t a, int *flag);
uint8_t Convert_int16_t_to_uint8_t(int16_t a, int *flag);
uint8_t Convert_int32_t_to_uint8_t(int32_t a, int *flag);
uint8_t Convert_int64_t_to_uint8_t(int64_t a, int *flag);
uint8_t Convert_intptr_t_to_uint8_t(intptr_t a, int *flag);
uint16_t Convert_int32_t_to_uint16_t(int32_t a, int *flag);
uint16_t Convert_int64_t_to_uint16_t(int64_t a, int *flag);
uint16_t Convert_intptr_t_to_uint16_t(intptr_t a, int *flag);
uint32_t Convert_int64_t_to_uint32_t(int64_t a, int *flag);
uint32_t Convert_intptr_t_to_uint32_t(intptr_t a, int *flag);
uint64_t Convert_intptr_t_to_uint64_t(intptr_t a, int *flag);
intptr_t Convert_int64_t_to_intptr_t(int64_t a, int *flag);
uintptr_t Convert_int64_t_to_uintptr_t(int64_t a, int *flag);

int8_t Convert_uint16_t_to_int8_t(uint16_t a, int *flag);
int8_t Convert_uint32_t_to_int8_t(uint32_t a, int *flag);
int8_t Convert_uint64_t_to_int8_t(uint64_t a, int *flag);
int8_t Convert_uintptr_t_to_int8_t(uintptr_t a, int *flag);
int16_t Convert_uint32_t_to_int16_t(uint32_t a, int *flag);
int16_t Convert_uint64_t_to_int16_t(uint64_t a, int *flag);
int16_t Convert_uintptr_t_to_int16_t(uintptr_t a, int *flag);
int32_t Convert_uint64_t_to_int32_t(uint64_t a, int *flag);
int32_t Convert_uintptr_t_to_int32_t(uintptr_t a, int *flag);
int64_t Convert_uintptr_t_to_int64_t(uintptr_t a, int *flag);
uint8_t Convert_uint16_t_to_uint8_t(uint16_t a, int *flag);
uint8_t Convert_uint32_t_to_uint8_t(uint32_t a, int *flag);
uint8_t Convert_uint64_t_to_uint8_t(uint64_t a, int *flag);
uint8_t Convert_uintptr_t_to_uint8_t(uintptr_t a, int *flag);
uint16_t Convert_uint32_t_to_uint16_t(uint32_t a, int *flag);
uint16_t Convert_uint64_t_to_uint16_t(uint64_t a, int *flag);
uint16_t Convert_uintptr_t_to_uint16_t(uintptr_t a, int *flag);
uint32_t Convert_uint64_t_to_uint32_t(uint64_t a, int *flag);
uint32_t Convert_uintptr_t_to_uint32_t(uintptr_t a, int *flag);
uint64_t Convert_uintptr_t_to_uint64_t(uintptr_t a, int *flag);
intptr_t Convert_uint64_t_to_intptr_t(uint64_t a, int *flag);
uintptr_t Convert_uint64_t_to_uintptr_t(uint64_t a, int *flag);