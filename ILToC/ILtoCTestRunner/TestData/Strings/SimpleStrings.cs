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
public partial class SimpleStrings {
  public static int Main() {

    // Declare without initializing.
    string message1;

    // Initialize to null.
    string message2 = null;

    // Initialize as an empty string.
    // Use the Empty constant instead of the literal "".
    string message3 = System.String.Empty;

    //Initialize with a regular string literal.
    string oldPath = "c:\\Program Files\\Microsoft Visual Studio 8.0";

    // TODO The \'s are not escaped.
    // Initialize with a verbatim string literal.
    string newPath = @"c:\Program Files\Microsoft Visual Studio 9.0";

    // Use System.String if you prefer.
    System.String greeting = "Hello World!";

    // In local variables (i.e. within a method body)
    // you can use implicit typing.
    var temp = "I'm still a strongly-typed System.String!";

    // Use a const string to prevent 'message4' from
    // being used to store another string value.
    const string message4 = "You can't get rid of me!";

    // Use the String constructor only when creating a string from a char*, char[], or sbyte*. See System.String documentation for details.
    // TODO Causes compilation failure
    char[] letters = { 'A', 'B', 'C' };
    string alphabet1 = new string(letters);

    string alphabet2 = "ABC";
    int result = alphabet1 == alphabet2 ? 0 : 1;

    // TODO Causes runtime failure
    string s1 = new string('A', 5);
    return 0;
  }
}

