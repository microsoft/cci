// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.SmallBasic.Library
{
    using System;

    /// <summary>
    /// The TextHelper class provides helpers for working with Text.
    /// </summary>
    public static class TextHelper
    {
        /// <summary>
        /// Gets the length of the given text.
        /// </summary>
        /// <param name="text">
        /// The text whose length is needed
        /// </param>
        /// <returns>
        /// The length of the given text
        /// </returns>
        public static decimal GetLength(string text)
        {
            if (text == null)
                return 0;
            else
                return (decimal)text.Length;
        }

        /// <summary>
        /// Gets whether or not a given subText is a subset of the larger text.
        /// </summary>
        /// <param name="text">
        /// The larger text within which the sub-text will be searched
        /// </param>
        /// <param name="subText">
        /// The sub-text to search for
        /// </param>
        /// <returns>
        /// True if the subtext was found within the given text
        /// </returns>
        public static bool IsSubText(string text, string subText)
        {
            if (text == null || subText == null)
                return false;
            else
                return text.Contains(subText);
        }

        /// <summary>
        /// Gets whether or not a given text ends with the specified subText.
        /// </summary>
        /// <param name="text">
        /// The lareger text to search within
        /// </param>
        /// <param name="subText">
        /// The sub-text to search for
        /// </param>
        /// <returns>
        /// True if the subtext was found at the end of the given text
        /// </returns>
        public static bool EndsWith(string text, string subText)
        {
            if (text == null || subText == null)
                return false;
            else
                return text.EndsWith(subText);
        }

        /// <summary>
        /// Gets whether or not a given text starts with the specified subText.
        /// </summary>
        /// <param name="text">
        /// The lareger text to search within
        /// </param>
        /// <param name="subText">
        /// The sub-text to search for
        /// </param>
        /// <returns>
        /// True if the subtext was found at the start of the given text
        /// </returns>
        public static bool StartsWith(string text, string subText)
        {
            if (text == null || subText == null)
                return false;
            else
                return text.StartsWith(subText);
        }

        /// <summary>
        /// Gets a sub-text from the given text.
        /// </summary>
        /// <param name="text">
        /// The text to derive the sub-text from
        /// </param>
        /// <param name="start">
        /// Specifies where to start from
        /// </param>
        /// <param name="length">
        /// Specifies the length of the sub text
        /// </param>
        /// <returns>
        /// The requested sub-text
        /// </returns>
        public static string GetSubText(string text, decimal start, decimal length)
        {
            if (text == null || start > text.Length)
                return "";
            else
                return text.Substring((int)start, (int)length);
        }

        /// <summary>
        /// Gets a sub-text from the given text.
        /// </summary>
        /// <param name="text">
        /// The text to derive the sub-text from
        /// </param>
        /// <param name="start">
        /// Specifies where to start from
        /// </param>
        /// <returns>
        /// The requested sub-text
        /// </returns>
        public static string GetSubText(string text, decimal start)
        {
            if (text == null || start > text.Length)
                return "";
            else
                return text.Substring((int)start);
        }

        /// <summary>
        /// Converts the given text to lower case.
        /// </summary>
        /// <param name="text">
        /// The text to convert to lower case
        /// </param>
        /// <returns>
        /// The lower case version of the given text
        /// </returns>
        public static string ConvertToLowerCase(string text)
        {
            if (text == null)
                return "";
            else
                return text.ToLowerInvariant();
        }

        /// <summary>
        /// Converts the given text to upper case.
        /// </summary>
        /// <param name="text">
        /// The text to convert to upper case
        /// </param>
        /// <returns>
        /// The upper case version of the given text
        /// </returns>
        public static string ConvertToUpperCase(string text)
        {
            if (text == null)
                return "";
            else
                return text.ToUpperInvariant();
        }

        /// <summary>
        /// Converts a value (say, a number) to the text format.
        /// </summary>
        /// <param name="value">
        /// Any value that needs to be converted to text
        /// </param>
        /// <returns>
        /// A text representation of the given value
        /// </returns>
        public static string GetText(object value)
        {
            if (value == null)
                return "";
            else
                return value.ToString();
        }
    }
}