// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.SmallBasic.Library
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;

    /// <summary>
    /// The mouse class provides accessors to get or set the mouse related properties, like the cursor
    /// position, pointer, etc.
    /// </summary>
    public static class Mouse
    {
        /// <summary>
        /// Hides the mouse cursor on the screen.
        /// </summary>
        public static void HideCursor()
        {
            Cursor.Hide();
        }

        /// <summary>
        /// Shows the mouse cursors on the screen.
        /// </summary>
        public static void ShowCursor()
        {
            Cursor.Show();
        }

        /// <summary>
        /// Gets or sets the mouse cursor's x co-ordinate.
        /// </summary>
        public static decimal MouseX
        {
            get
            {
                return Cursor.Position.X;
            }

            set
            {
                Cursor.Position = new Point((int)value, (int)MouseY);
            }
        }

        /// <summary>
        /// Gets or sets the mouse cursor's y co-ordinate.
        /// </summary>
        public static decimal MouseY
        {
            get
            {
                return Cursor.Position.Y;
            }

            set
            {
                Cursor.Position = new Point((int)MouseX, (int)value);
            }
        }

        /// <summary>
        /// Gets whether or not the left button is pressed.
        /// </summary>
        public static bool IsLeftButtonDown
        {
            get
            {
                return (Control.MouseButtons & MouseButtons.Left) == MouseButtons.Left;
            }
        }

        /// <summary>
        /// Gets whether or not the right button is pressed.
        /// </summary>
        public static bool IsRightButtonDown
        {
            get
            {
                return (Control.MouseButtons & MouseButtons.Right) == MouseButtons.Right;
            }
        }
    }
}