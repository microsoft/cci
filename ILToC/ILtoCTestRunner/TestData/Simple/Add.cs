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
using System;

    public static class Add
    {
        public static int Main()
        {
          return DoAdd(1, 2) == 3 ? 0 : 1;
        }

        static int DoAdd(int x, int y) {
          return x + y;
        }

    }

