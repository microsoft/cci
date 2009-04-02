// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.SmallBasic.Library
{
    using System;

    /// <summary>
    /// The Math class provides lots of useful mathematics related methods
    /// </summary>
    public static class Math
    {
        /// <summary>
        /// Gets the absolute value of the given number.  For example, -32.233 will return 32.233.
        /// </summary>
        /// <param name="number">
        /// The number to get the absolute value for
        /// </param>
        /// <returns>
        /// The absolute value of the given number
        /// </returns>
        public static decimal Abs(decimal number)
        {
            return System.Math.Abs(number);
        }

        /// <summary>
        /// Gets an integer that is greater than or equal to the specified decimal number.  For example,
        /// 32.233 will return 33.
        /// </summary>
        /// <param name="number">
        /// The number whose ceiling is required
        /// </param>
        /// <returns>
        /// The ceiling value of the given number
        /// </returns>
        public static decimal Ceiling(decimal number)
        {
            return System.Math.Ceiling(number);
        }

        /// <summary>
        /// Gets an integer that is less than or equal to the specified decimal number.  For example,
        /// 32.233 will return 32.
        /// </summary>
        /// <param name="number">
        /// The number whose floor value is required
        /// </param>
        /// <returns>
        /// The floor value of the given number
        /// </returns>
        public static decimal Floor(decimal number)
        {
            return System.Math.Floor(number);
        }

        /// <summary>
        /// Gets the natural logarithm value of the given number.
        /// </summary>
        /// <param name="number">
        /// The number whose natural logarithm value is required
        /// </param>
        /// <returns>
        /// The natural log value of the given number
        /// </returns>
        public static decimal NaturalLog(decimal number)
        {
            return (decimal)System.Math.Log((double)number);
        }

        /// <summary>
        /// Gets the logarithm (base 10) value of the given number.
        /// </summary>
        /// <param name="number">
        /// The number whose logarithm value is required
        /// </param>
        /// <returns>
        /// The log value of the given number
        /// </returns>
        public static decimal Log(decimal number)
        {
            return (decimal)System.Math.Log10((double)number);
        }

        /// <summary>
        /// Gets the cosine of the given angle in radians.
        /// </summary>
        /// <param name="angle">
        /// The angle whose cosine is needed (in radians)
        /// </param>
        /// <returns>
        /// The cosine of the given angle
        /// </returns>
        public static decimal Cos(decimal angle)
        {
            return (decimal)System.Math.Cos((double)angle);
        }

        /// <summary>
        /// Gets the sine of the given angle in radians.
        /// </summary>
        /// <param name="angle">
        /// The angle whose sine is needed (in radians)
        /// </param>
        /// <returns>
        /// The sine of the given angle
        /// </returns>
        public static decimal Sin(decimal angle)
        {
            return (decimal)System.Math.Sin((double)angle);
        }

        /// <summary>
        /// Gets the tangent of the given angle in radians.
        /// </summary>
        /// <param name="angle">
        /// The angle whose tangent is needed (in radians)
        /// </param>
        /// <returns>
        /// The tangent of the given angle
        /// </returns>
        public static decimal Tan(decimal angle)
        {
            return (decimal)System.Math.Tan((double)angle);
        }

        /// <summary>
        /// Converts a given angle in radians to degrees.
        /// </summary>
        /// <param name="angle">
        /// The angle in radians
        /// </param>
        /// <returns>
        /// The converted angle in degrees
        /// </returns>
        public static decimal GetDegrees(decimal angle)
        {
            return (decimal)((180 * (double)angle / System.Math.PI) % 360);
        }

        /// <summary>
        /// Converts a given angle in degrees to radians.
        /// </summary>
        /// <param name="angle">
        /// The angle in degress
        /// </param>
        /// <returns>
        /// The converted angle in radians
        /// </returns>
        public static decimal GetRadians(decimal angle)
        {
            return (decimal)(((double)angle % 360) * System.Math.PI / 180);
        }

        /// <summary>
        /// Gets the square root of a given number.
        /// </summary>
        /// <param name="number">
        /// The number whose square root value is needed
        /// </param>
        /// <returns>
        /// The square root value of the given number
        /// </returns>
        public static decimal SquareRoot(decimal number)
        {
            return (decimal)System.Math.Sqrt((double)number);
        }

        /// <summary>
        /// Rounds a given number to the nearest integer.  For example 32.233 will be rounded to 32.0 while 
        /// 32.566 will be rounded to 33.
        /// </summary>
        /// <param name="number">
        /// The number whose approximation is required
        /// </param>
        /// <returns>
        /// The rounded value of the given number
        /// </returns>
        public static decimal Round(decimal number)
        {
            return System.Math.Round(number);
        }

        /// <summary>
        /// Compares two numbers and returns the greater of the two.
        /// </summary>
        /// <param name="number1">
        /// The first of the two numbers to compare
        /// </param>
        /// <param name="number2">
        /// The second of the two numbers to compare
        /// </param>
        /// <returns>
        /// The greater value of the two numbers
        /// </returns>
        public static decimal Max(decimal number1, decimal number2)
        {
            return System.Math.Max(number1, number2);
        }

        /// <summary>
        /// Compares two numbers and returns the smaller of the two.
        /// </summary>
        /// <param name="number1">
        /// The first of the two numbers to compare
        /// </param>
        /// <param name="number2">
        /// The second of the two numbers to compare
        /// </param>
        /// <returns>
        /// The smaller value of the two numbers
        /// </returns>
        public static decimal Min(decimal number1, decimal number2)
        {
            return System.Math.Min(number1, number2);
        }
    }
}