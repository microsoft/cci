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
//This file contains simple functions that are platform independent but cannot be written in C#.
#include <stdint.h>

uint32_t Increment_and_align(uint32_t offset, uint32_t increment) {
  offset += increment;
  while (offset % sizeof(void*) != 0)
    offset++;
  return offset; 
}
