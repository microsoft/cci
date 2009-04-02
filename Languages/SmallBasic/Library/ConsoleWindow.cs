// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.SmallBasic.Library
{
    using System;
    using System.Text;

    /// <summary>
    /// The ConsoleWindow provides text-related input and output functionalities.  For example using this 
    /// class, it is possible to write or read some text or number to and from the text-based console
    /// window.
    /// </summary>
    public static class ConsoleWindow
    {
        #region Private Fields

        static bool _windowVisible = false;

        #endregion // Private Fields

        /// <summary>
        /// Shows the console window to enable interactions with it.
        /// </summary>
        public static void Show()
        {
            if (!_windowVisible)
            {
                if (NativeHelper.AllocConsole())
                    _windowVisible = true;
            }
        }

        /// <summary>
        /// Hides the console window.
        /// </summary>
        public static void Hide()
        {
            if (_windowVisible)
            {
                if (NativeHelper.FreeConsole())
                    _windowVisible = false;
            }
        }

        /// <summary>
        /// Reads a line of text from the console window.  This function will not return until the user
        /// hits ENTER.
        /// </summary>
        /// <returns>
        /// The text that was read from the console window
        /// </returns>
        public static string ReadText()
        {
            VerifyAccess();
            return Console.ReadLine();
        }

        /// <summary>
        /// Reads a number from the console window.  This function will not return until the user hits 
        /// ENTER.
        /// </summary>
        /// <returns>
        /// The number that was read from the console window
        /// </returns>
        public static double ReadNumber()
        {
            VerifyAccess();

            StringBuilder number = new StringBuilder();
            bool dotEntered = false;
            int index = 0;
            while (true)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                char keyChar = keyInfo.KeyChar;
                bool isValid = false;

                if (keyChar == '-' && index == 0)
                {
                    isValid = true;
                }
                else if (keyChar == '.' && !dotEntered)
                {
                    dotEntered = true;
                    isValid = true;
                }
                else if (keyChar >= '0' && keyChar <= '9')
                {
                    isValid = true;
                }

                if (isValid)
                {
                    Console.Write(keyChar);
                    number.Append(keyChar);
                    index++;
                }
                else if (index > 0 && keyInfo.Key == ConsoleKey.Backspace)
                {
                    // Backspace one character
                    Console.CursorLeft = Console.CursorLeft - 1;
                    Console.Write(" ");
                    Console.CursorLeft = Console.CursorLeft - 1;

                    index--;
                    keyChar = number[index];
                    if (keyChar == '.')
                        dotEntered = false;
                    number.Remove(index, 1);
                }
                else if (keyInfo.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
            }

            if (number.Length == 0)
                return 0;

            return double.Parse(number.ToString());
        }

        /// <summary>
        /// Writes text or number to the console window.  A new line character will be appended to the output, 
        /// so that the next time something is written to the console window, it will go in a new line.
        /// </summary>
        /// <param name="data">
        /// The text or number to write to the console window
        /// </param>
        public static void WriteLine(object data)
        {
            VerifyAccess();
            Console.WriteLine(data);
        }

        /// <summary>
        /// Writes text or number to the console window.  Unlike WriteLine, this will not append a new line
        /// character, which means, anything written to the console window after this call will be on the
        /// same line.
        /// </summary>
        /// <param name="data">
        /// The text or number to write to the console window
        /// </param>
        public static void Write(object data)
        {
            VerifyAccess();
            Console.Write(data);
          }

        /// <summary>
        /// Gets or sets the foreground color of the text to be output in the console window.
        /// </summary>
        public static ConsoleTextColor ForegroundColor
        {
            get
            {
                VerifyAccess();
                return (ConsoleTextColor)Console.ForegroundColor;
            }

            set
            {
                VerifyAccess();
                Console.ForegroundColor = (ConsoleColor)value;
            }
        }

        /// <summary>
        /// Gets or sets the background color of the text to be output in the console window.
        /// </summary>
        public static ConsoleTextColor BackgroundColor
        {
            get
            {
                VerifyAccess();
                return (ConsoleTextColor)Console.BackgroundColor;
            }
            
            set
            {
                VerifyAccess();
                Console.BackgroundColor = (ConsoleColor)value;
            }
        }

        /// <summary>
        /// Gets or sets the cursor's column position on the console window.
        /// </summary>
        public static decimal CursorLeft
        {
            get
            {
                VerifyAccess();
                return Console.CursorLeft;
            }

            set
            {
                VerifyAccess();
                Console.CursorLeft = (int)value;
            }
        }

        /// <summary>
        /// Gets or sets the cursor's row position on the console window.
        /// </summary>
        public static decimal CursorTop
        {
            get
            {
                VerifyAccess();
                return Console.CursorTop;
            }

            set
            {
                VerifyAccess();
                Console.CursorTop = (int)value;
            }
        }

        /// <summary>
        /// Gets or sets the Title for the console window.
        /// </summary>
        public static string Title
        {
            get
            {
                VerifyAccess();
                return Console.Title;
            }

            set
            {
                VerifyAccess();
                Console.Title = value;
            }
        }

        #region Private Helpers

        /// <summary>
        /// Verifies if the access to Console Window has been made yet
        /// </summary>
        static void VerifyAccess()
        {
            // Show the window if it's not visible yet
            if (!_windowVisible)
                Show();
        }

        #endregion // Private Helpers
    }
}