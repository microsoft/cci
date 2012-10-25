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

int32_t Add_int32_t_int32_t(int32_t lhs, int32_t rhs, int *flag) {
	int64_t tmp = (int64_t)lhs + (int64_t)rhs;
	if (tmp <= INT32_MAX && tmp >= INT32_MIN) {
		*flag = 0;
		return tmp;
	}
	*flag = 1;
	return 0;
}

int64_t Add_int64_t_int64_t(int64_t lhs, int64_t rhs, int *flag) {
	int64_t tmp = (int64_t)((uint64_t)lhs + (uint64_t)rhs);
	if (lhs >= 0) {

		// mixed sign cannot overflow
		if ( rhs >= 0 && tmp < lhs) {
			*flag = 1;
			return 0;
		} 
	}else {
		// lhs negative
		if( rhs < 0 && tmp > lhs ) {
			*flag = 1;
			return 0;
		}
	}
	*flag = 0;
	return tmp;
}

intptr_t Add_int32_t_intptr_t(int32_t lhs, intptr_t rhs, int *flag) {
	int64_t tmp = Add_int64_t_int64_t((int64_t)lhs, (int64_t)rhs, flag);
	if (*flag == 0) {
		if (tmp <= INTPTR_MAX && tmp >= INTPTR_MIN) {
			*flag = 0;
			return (intptr_t)tmp;
		}		
	} 
	*flag = 1;
	return 0;
}

intptr_t Add_intptr_t_intptr_t(intptr_t lhs, intptr_t rhs, int *flag) {
	int64_t tmp = Add_int64_t_int64_t((int64_t)lhs, (int64_t)rhs, flag);
	if (*flag == 0) {
		if (tmp <= INTPTR_MAX && tmp >= INTPTR_MIN) {
			*flag = 0;
			return (intptr_t)tmp;
		}		
	} 
	*flag = 1;
	return 0;
}

uint32_t Add_uint32_t_uint32_t(uint32_t lhs, uint32_t rhs, int *flag) {
	uint32_t tmp = lhs + rhs;
	if (tmp >= rhs && tmp <= UINT32_MAX) {
		*flag = 0;
		return tmp;
	}
	*flag = 1;
	return 0;
}

uint64_t Add_uint64_t_uint64_t(uint64_t lhs, uint64_t rhs, int *flag) {
	uint64_t tmp = lhs + rhs;
	if (tmp >= rhs && tmp <= UINT64_MAX) {
		*flag = 0;
		return tmp;
	}
	*flag = 1;
	return 0;
}

uintptr_t Add_uint32_t_uintptr_t(uint32_t lhs, uintptr_t rhs, int *flag) {
	uint64_t tmp = Add_uint64_t_uint64_t((uint64_t)lhs, (uint64_t)rhs, flag);
	if (*flag == 0) {
		if (tmp <= UINTPTR_MAX) {
			*flag = 0;
			return (uintptr_t)tmp;
		}		
	} 
	*flag = 1;
	return 0;
}

uintptr_t Add_uintptr_t_uintptr_t(uintptr_t lhs, uintptr_t rhs, int *flag) {
	uint64_t tmp = Add_uint64_t_uint64_t((uint64_t)lhs, (uint64_t)rhs, flag);
	if (*flag == 0) {
		if (tmp <= UINTPTR_MAX) {
			*flag = 0;
			return (uintptr_t)tmp;
		}		
	} 
	*flag = 1;
	return 0;
}

int32_t Subtract_int32_t_int32_t(int32_t lhs, int32_t rhs, int *flag) {
	int64_t tmp = (int64_t)lhs + (int64_t)rhs;
	if (tmp <= INT32_MAX && tmp >= INT32_MIN) {
		*flag = 0;
		return tmp;
	}
	*flag = 1;
	return 0;
}

int64_t Subtract_int64_t_int64_t(int64_t lhs, int64_t rhs, int *flag) {
	int64_t tmp = (int64_t)((uint64_t)lhs + (uint64_t)rhs);
	if (lhs >= 0) {

		// mixed sign cannot overflow
		if ( rhs >= 0 && tmp < lhs) {
			*flag = 1;
			return 0;
		} 
	}else {
		// lhs negative
		if( rhs < 0 && tmp > lhs ) {
			*flag = 1;
			return 0;
		}
	}
	*flag = 0;
	return tmp;
}

intptr_t Subtract_int32_t_intptr_t(int32_t lhs, intptr_t rhs, int *flag) {
	int64_t tmp = Subtract_int64_t_int64_t((int64_t)lhs, (int64_t)rhs, flag);
	if (*flag == 0) {
		if (tmp <= INTPTR_MAX && tmp >= INTPTR_MIN) {
			*flag = 0;
			return (intptr_t)tmp;
		}		
	} 
	*flag = 1;
	return 0;
}

intptr_t Subtract_intptr_t_intptr_t(intptr_t lhs, intptr_t rhs, int *flag) {
	int64_t tmp = Subtract_int64_t_int64_t((int64_t)lhs, (int64_t)rhs, flag);
	if (*flag == 0) {
		if (tmp <= INTPTR_MAX && tmp >= INTPTR_MIN) {
			*flag = 0;
			return (intptr_t)tmp;
		}		
	} 
	*flag = 1;
	return 0;
}

uint32_t Subtract_uint32_t_uint32_t(uint32_t lhs, uint32_t rhs, int *flag) {
	// both are unsigned - easy case
    if( rhs <= lhs )
    {
		*flag = 0;
        return lhs - rhs;
    }
	*flag = 1;
	return 0;
}

uint64_t Subtract_uint64_t_uint64_t(uint64_t lhs, uint64_t rhs, int *flag) {
	// both are unsigned - easy case
    if( rhs <= lhs )
    {
		*flag = 0;
        return lhs - rhs;
    }
	*flag = 1;
	return 0;
}

uintptr_t Subtract_uint32_t_uintptr_t(uint32_t lhs, uintptr_t rhs, int *flag) {
	uint64_t tmp = Subtract_uint64_t_uint64_t((uint64_t)lhs, (uint64_t)rhs, flag);
	if (*flag == 0) {
		if (tmp <= UINTPTR_MAX) {
			*flag = 0;
			return (uintptr_t)tmp;
		}		
	} 
	*flag = 1;
	return 0;
}

uintptr_t Subtract_uintptr_t_uintptr_t(uintptr_t lhs, uintptr_t rhs, int *flag) {
	uint64_t tmp = Subtract_uint64_t_uint64_t((uint64_t)lhs, (uint64_t)rhs, flag);
	if (*flag == 0) {
		if (tmp <= UINTPTR_MAX) {
			*flag = 0;
			return (uintptr_t)tmp;
		}		
	} 
	*flag = 1;
	return 0;
}

uint64_t getAbsoluteValue_int64_t(int64_t t) {
	return ~(uint64_t)t + 1;
}

uint32_t getAbsoluteValue_int32_t(int32_t t) {
	return ~(uint32_t)t + 1;
}

int64_t SignedNegation_int64_t(uint64_t val) {
	return (int64_t)(~val + 1);
}

uint64_t Multiply_uint64_t_uint64_t(uint64_t a, uint64_t b, int *flag) {

	uint32_t aHigh, aLow, bHigh, bLow;
	uint64_t pRet = 0;
	
	// Consider that a*b can be broken up into:
	// (aHigh * 2^32 + aLow) * (bHigh * 2^32 + bLow)
	// => (aHigh * bHigh * 2^64) + (aLow * bHigh * 2^32) + (aHigh * bLow * 2^32) + (aLow * bLow)
	// Note - same approach applies for 128 bit math on a 64-bit system
	
	aHigh = (uint32_t)(a >> 32);
	aLow  = (uint32_t)a;
	bHigh = (uint32_t)(b >> 32);
	bLow  = (uint32_t)b;

	if(aHigh == 0) {
		if(bHigh != 0) {
			pRet = (uint64_t)aLow * (uint64_t)bHigh;
		}
	}
	else if(bHigh == 0) {
		if(aHigh != 0) {        
			pRet = (uint64_t)aHigh * (uint64_t)bLow;
		}
	}
	else {
		*flag = 1;
		return 0;
	}
	
	if(pRet != 0) {
		uint64_t tmp;
		
		if((uint32_t)(pRet >> 32) != 0) {
			*flag = 1;
			return 0;
		}
		
		pRet <<= 32;
		tmp = (uint64_t)aLow * (uint64_t)bLow;
		pRet += tmp;
		
		if(pRet < tmp) {
			*flag = 1;
			return 0;
		}
		
		*flag = 0;
	}
	
	*flag = 0;
	return (uint64_t)aLow * (uint64_t)bLow;
	
}

int64_t Multiply_int64_t_int64_t(int64_t a, int64_t b, int *flag) {

	int aNegative = 0;
	int bNegative = 0;
	
	uint64_t tmp;
	int64_t a1 = a;
	int64_t b1 = b;

	if( a1 < 0 ) {
		aNegative = 1;
		a1 = (int64_t)getAbsoluteValue_int64_t(a1);
	}
	
	if( b1 < 0 ) {
		bNegative = 1;
		b1 = (int64_t)getAbsoluteValue_int64_t(b1);
	}
	
	tmp = Multiply_uint64_t_uint64_t( (uint64_t)a1, (uint64_t)b1, flag );
	
	if( *flag == 0 ) {
		// The unsigned multiplication didn't overflow
		if( aNegative ^ bNegative ) {
			// Result must be negative
			if( tmp <= (uint64_t)INT64_MIN ) {
				*flag = 0;
				return SignedNegation_int64_t(tmp);
			}
		}
		else {
			// Result must be positive
			if( tmp <= (uint64_t)INT64_MAX ) {
				*flag = 0;
				return (int64_t)tmp;
			}
		}
	}
	
	*flag = 1;
	return 0;
}

int32_t Multiply_int32_t_int32_t(int32_t a, int32_t b, int *flag) {
	int64_t tmp = Multiply_int64_t_int64_t((int64_t)a, (int64_t)b, flag);
	if( *flag == 0 && tmp >= INT32_MIN && tmp <= INT32_MAX ) {
		*flag = 0;
		return (int32_t)tmp;
	}	
	*flag = 1;
	return 0;
}

intptr_t Multiply_int32_t_intptr_t(int32_t a, intptr_t b, int *flag) {
	int64_t tmp = Multiply_int64_t_int64_t((int64_t)a, (int64_t)b, flag);
	if( *flag == 0 && tmp >= INTPTR_MIN && tmp <= INTPTR_MAX ) {
		*flag = 0;
		return (intptr_t)tmp;
	}	
	*flag = 1;
	return 0;
}

intptr_t Multiply_intptr_t_intptr_t(intptr_t a, intptr_t b, int *flag) {
	int64_t tmp = Multiply_int64_t_int64_t((int64_t)a, (int64_t)b, flag);
	if( *flag == 0 && tmp >= INTPTR_MIN && tmp <= INTPTR_MAX ) {
		*flag = 0;
		return (intptr_t)tmp;
	}	
	*flag = 1;
	return 0;
}

uint32_t Multiply_uint32_t_uint32_t(uint32_t a, uint32_t b, int *flag) {
	uint64_t tmp = Multiply_uint64_t_uint64_t((uint64_t)a, (uint64_t)b, flag);
	if( *flag == 0 && tmp <= UINT32_MAX ) {
		*flag = 0;
		return (uint32_t)tmp;
	}	
	*flag = 1;
	return 0;
}

uintptr_t Multiply_uint32_t_uintptr_t(uint32_t a, uintptr_t b, int *flag) {
	uint64_t tmp = Multiply_uint64_t_uint64_t((uint64_t)a, (uint64_t)b, flag);
	if( *flag == 0 && tmp <= UINTPTR_MAX ) {
		*flag = 0;
		return (uintptr_t)tmp;
	}	
	*flag = 1;
	return 0;
}

uintptr_t Multiply_uintptr_t_uintptr_t(uintptr_t a, uintptr_t b, uintptr_t *pRet, int *flag) {
	uint64_t tmp = Multiply_uint64_t_uint64_t((uint64_t)a, (uint64_t)b, flag);
	if( *flag == 0 && tmp <= UINTPTR_MAX ) {
		*flag = 0;
		return (uintptr_t)tmp;
	}	
	*flag = 1;
	return 0;
}

int8_t Convert_int16_t_to_int8_t(int16_t a, int *flag) {
	if( a > INT8_MAX || a < INT8_MIN ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (int8_t) a;
}

int8_t Convert_int32_t_to_int8_t(int32_t a, int *flag) {
	if( a > INT8_MAX || a < INT8_MIN ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (int8_t) a;
}

int8_t Convert_int64_t_to_int8_t(int64_t a, int *flag) {
	if( a > INT8_MAX || a < INT8_MIN ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (int8_t) a;
}

int8_t Convert_intptr_t_to_int8_t(intptr_t a, int *flag) {
	if( a > INT8_MAX || a < INT8_MIN ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (int8_t) a;
}

int16_t Convert_int32_t_to_int16_t(int32_t a, int *flag) {
	if( a > INT16_MAX || a < INT16_MIN ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (int16_t) a;
}

int16_t Convert_int64_t_to_int16_t(int64_t a, int *flag) {
	if( a > INT16_MAX || a < INT16_MIN ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (int16_t) a;
}

int16_t Convert_intptr_t_to_int16_t(intptr_t a, int *flag) {
	if( a > INT16_MAX || a < INT16_MIN ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (int16_t) a;
}

int32_t Convert_int64_t_to_int32_t(int64_t a, int *flag) {
	if( a > INT32_MAX || a < INT32_MIN ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (int32_t) a;
}

int32_t Convert_intptr_t_to_int32_t(intptr_t a, int *flag) {
	if( a > INT32_MAX || a < INT32_MIN ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (int32_t) a;
}

int64_t Convert_intptr_t_to_int64_t(intptr_t a, int *flag) {
	if( a > INT64_MAX || a < INT64_MIN ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (int64_t) a;
}

uint8_t Convert_int16_t_to_uint8_t(int16_t a, int *flag) {
	if( a > UINT8_MAX || a < 0 ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (uint8_t) a;
}

uint8_t Convert_int32_t_to_uint8_t(int32_t a, int *flag) {
	if( a > UINT8_MAX || a < 0 ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (uint8_t) a;
}

uint8_t Convert_int64_t_to_uint8_t(int64_t a, int *flag) {
	if( a > UINT8_MAX || a < 0 ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (uint8_t) a;
}

uint8_t Convert_intptr_t_to_uint8_t(intptr_t a, int *flag) {
	if( a > UINT8_MAX || a < 0 ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (uint8_t) a;
}

uint16_t Convert_int32_t_to_uint16_t(int32_t a, int *flag) {
	if( a > UINT16_MAX || a < 0 ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (uint16_t) a;
}

uint16_t Convert_int64_t_to_uint16_t(int64_t a, int *flag) {
	if( a > UINT16_MAX || a < 0 ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (uint16_t) a;
}

uint16_t Convert_intptr_t_to_uint16_t(intptr_t a, int *flag) {
	if( a > UINT16_MAX || a < 0 ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (uint16_t) a;
}

uint32_t Convert_int64_t_to_uint32_t(int64_t a, int *flag) {
	if( a > UINT32_MAX || a < 0 ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (uint32_t) a;
}

uint32_t Convert_intptr_t_to_uint32_t(intptr_t a, int *flag) {
	if( a > UINT32_MAX || a < 0 ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (uint32_t) a;
}

uint64_t Convert_intptr_t_to_uint64_t(intptr_t a, int *flag) {
	if( a > UINT64_MAX || a < 0 ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (uint64_t) a;
}

intptr_t Convert_int64_t_to_intptr_t(int64_t a, int *flag) {
	if( a > INTPTR_MAX || a < INTPTR_MIN	 ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (intptr_t) a;
}

uintptr_t Convert_int64_t_to_uintptr_t(int64_t a, int *flag) {
	if( a > UINTPTR_MAX || a < 0 ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (uintptr_t) a;
}

int8_t Convert_uint16_t_to_int8_t(uint16_t a, int *flag) {
	if( a > INT8_MAX || a < INT8_MIN ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (int8_t) a;
}

int8_t Convert_uint32_t_to_int8_t(uint32_t a, int *flag) {
	if( a > INT8_MAX || a < INT8_MIN ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (int8_t) a;
}

int8_t Convert_uint64_t_to_int8_t(uint64_t a, int *flag) {
	if( a > INT8_MAX || a < INT8_MIN ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (int8_t) a;
}

int8_t Convert_uintptr_t_to_int8_t(uintptr_t a, int *flag) {
	if( a > INT8_MAX || a < INT8_MIN ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (int8_t) a;
}

int16_t Convert_uint32_t_to_int16_t(uint32_t a, int *flag) {
	if( a > INT16_MAX || a < INT16_MIN ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (int16_t) a;
}

int16_t Convert_uint64_t_to_int16_t(uint64_t a, int *flag) {
	if( a > INT16_MAX || a < INT16_MIN ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (int16_t) a;
}

int16_t Convert_uintptr_t_to_int16_t(uintptr_t a, int *flag) {
	if( a > INT16_MAX || a < INT16_MIN ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (int16_t) a;
}

int32_t Convert_uint64_t_to_int32_t(uint64_t a, int *flag) {
	if( a > INT32_MAX || a < INT32_MIN ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (int32_t) a;
}

int32_t Convert_uintptr_t_to_int32_t(uintptr_t a, int *flag) {
	if( a > INT32_MAX || a < INT32_MIN ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (int32_t) a;
}

int64_t Convert_uintptr_t_to_int64_t(uintptr_t a, int *flag) {
	if( a > INT64_MAX || a < INT64_MIN ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (int64_t) a;
}

uint8_t Convert_uint16_t_to_uint8_t(uint16_t a, int *flag) {
	if( a > UINT8_MAX || a < 0 ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (uint8_t) a;
}

uint8_t Convert_uint32_t_to_uint8_t(uint32_t a, int *flag) {
	if( a > UINT8_MAX || a < 0 ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (uint8_t) a;
}

uint8_t Convert_uint64_t_to_uint8_t(uint64_t a, int *flag) {
	if( a > UINT8_MAX || a < 0 ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (uint8_t) a;
}

uint8_t Convert_uintptr_t_to_uint8_t(uintptr_t a, int *flag) {
	if( a > UINT8_MAX || a < 0 ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (uint8_t) a;
}

uint16_t Convert_uint32_t_to_uint16_t(uint32_t a, int *flag) {
	if( a > UINT16_MAX || a < 0 ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (uint16_t) a;
}

uint16_t Convert_uint64_t_to_uint16_t(uint64_t a, int *flag) {
	if( a > UINT16_MAX || a < 0 ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (uint16_t) a;
}

uint16_t Convert_uintptr_t_to_uint16_t(uintptr_t a, int *flag) {
	if( a > UINT16_MAX || a < 0 ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (uint16_t) a;
}

uint32_t Convert_uint64_t_to_uint32_t(uint64_t a, int *flag) {
	if( a > UINT32_MAX || a < 0 ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (uint32_t) a;
}

uint32_t Convert_uintptr_t_to_uint32_t(uintptr_t a, int *flag) {
	if( a > UINT32_MAX || a < 0 ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (uint32_t) a;
}

uint64_t Convert_uintptr_t_to_uint64_t(uintptr_t a, int *flag) {
	if( a > UINT64_MAX || a < 0 ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (uint64_t) a;
}

intptr_t Convert_uint64_t_to_intptr_t(uint64_t a, int *flag) {
	if( a > INTPTR_MAX || a < INTPTR_MIN	 ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (intptr_t) a;
}

uintptr_t Convert_uint64_t_to_uintptr_t(uint64_t a, int *flag) {
	if( a > UINTPTR_MAX || a < 0 ) {
		*flag = 1;
		return 0;
	}
	*flag = 0;
	return (uintptr_t) a;
}