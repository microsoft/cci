// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.SmallBasic.Library
{
    using System;

    /// <summary>
    /// The NumberHelper class provides helpers for working with Text.
    /// </summary>
    public static class NumberHelper
    {
        /// <summary>
        /// Converts the given text to the number form.
        /// </summary>
        /// <param name="text">
        /// The text to convert to number
        /// </param>
        /// <returns>
        /// A number that was converted from the text.  If the text had an unconvertable number, this method
        /// will return 0
        /// </returns>
        public static double ConvertTextToNumber(string text)
        {
            double value = 0.0;
            double.TryParse(text, out value);

            return value;
        }

        /// <summary>
        /// Converts the given number to the text form.
        /// </summary>
        /// <param name="number">
        /// The number to convert to text
        /// </param>
        /// <returns>
        /// A text representation of the given number
        /// </returns>
        public static string ConvertNumberToText(double number)
        {
            return number.ToString();
        }
    }
}