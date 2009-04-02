// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.SmallBasic.Library
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Threading;
    using System.Windows.Forms;

    /// <summary>
    /// The GraphicsWindow provides graphics related input and output functionality.  For example, using this
    /// class, it is possible to draw and fill circles and rectangles.
    /// </summary>
    public static class GraphicsWindow
    {
        #region Private Fields

        static bool _windowVisible = false;
        static bool _windowCreated = false;
        static Form _window;
        static Thread _windowThread;
        static Graphics _gfx, _cachedGfx;
        static Pen _pen;
        static Brush _brush;
        static Font _font;
        static Bitmap _currentPage;
        static Dictionary<string, Bitmap> _savedPages = new Dictionary<string, Bitmap>();
        static event SmallBasicCallback _mouseDown, _mouseUp, _mouseMove;
        static decimal _mouseX, _mouseY;
        
        const int _imageWidth = 640;
        const int _imageHeight = 480;
        const int _step = 20;

        #endregion // Private Fields

        /// <summary>
        /// Shows the console window to enable interactions with it.
        /// </summary>
        public static void Show()
        {
            if (!_windowCreated)
                CreateWindow();

            if (!_windowVisible)
            {
                _window.Invoke((EventHandler)delegate
                {
                    _window.Show();
                });
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
                _window.Invoke((EventHandler)delegate
                {
                    _window.Hide();
                });

                _windowVisible = false;
            }
        }

        /// <summary>
        /// Draws a rectangle on the screen using the selected Pen
        /// </summary>
        /// <param name="x">
        /// The x co-ordinate of the rectangle
        /// </param>
        /// <param name="y">
        /// The y co-ordinate of the rectantle
        /// </param>
        /// <param name="width">
        /// The width of the rectantle
        /// </param>
        /// <param name="height">
        /// The height of the rectangle
        /// </param>
        public static void DrawRectangle(decimal x, decimal y, decimal width, decimal height)
        {
            VerifyAccess();
            _gfx.DrawRectangle(_pen, (float)x, (float)y, (float)width, (float)height);
            _cachedGfx.DrawRectangle(_pen, (float)x, (float)y, (float)width, (float)height);
        }

        /// <summary>
        /// Fills a rectangle on the screen using the selected Brush
        /// </summary>
        /// <param name="x">
        /// The x co-ordinate of the rectangle
        /// </param>
        /// <param name="y">
        /// The y co-ordinate of the rectantle
        /// </param>
        /// <param name="width">
        /// The width of the rectantle
        /// </param>
        /// <param name="height">
        /// The height of the rectangle
        /// </param>
        public static void FillRectangle(decimal x, decimal y, decimal width, decimal height)
        {
            VerifyAccess();
            _gfx.FillRectangle(_brush, (float)x, (float)y, (float)width, (float)height);
            _cachedGfx.FillRectangle(_brush, (float)x, (float)y, (float)width, (float)height);
        }

        /// <summary>
        /// Draws an ellipse on the screen using the selected Pen.
        /// </summary>
        /// <param name="x">
        /// The x co-ordinate of the ellipse
        /// </param>
        /// <param name="y">
        /// The y co-ordinate of the ellipse
        /// </param>
        /// <param name="width">
        /// The width of the ellipse
        /// </param>
        /// <param name="height">
        /// The height of the ellipse
        /// </param>
        public static void DrawEllipse(decimal x, decimal y, decimal width, decimal height)
        {
            VerifyAccess();
            _gfx.DrawEllipse(_pen, (float)x, (float)y, (float)width, (float)height);
            _cachedGfx.DrawEllipse(_pen, (float)x, (float)y, (float)width, (float)height);
        }

        /// <summary>
        /// Fills an ellipse on the screen using the selected Pen.
        /// </summary>
        /// <param name="x">
        /// The x co-ordinate of the ellipse
        /// </param>
        /// <param name="y">
        /// The y co-ordinate of the ellipse
        /// </param>
        /// <param name="width">
        /// The width of the ellipse
        /// </param>
        /// <param name="height">
        /// The height of the ellipse
        /// </param>
        public static void FillEllipse(decimal x, decimal y, decimal width, decimal height)
        {
            VerifyAccess();
            _gfx.FillEllipse(_brush, (float)x, (float)y, (float)width, (float)height);
            _cachedGfx.FillEllipse(_brush, (float)x, (float)y, (float)width, (float)height);
        }

        /// <summary>
        /// Draws a line from one point to another.
        /// </summary>
        /// <param name="x1">
        /// The x co-ordinate of the first point
        /// </param>
        /// <param name="y1">
        /// The y co-ordinate of the first point
        /// </param>
        /// <param name="x2">
        /// The x co-ordinate of the second point
        /// </param>
        /// <param name="y2">
        /// The y co-ordinate of the second point
        /// </param>
        public static void DrawLine(decimal x1, decimal y1, decimal x2, decimal y2)
        {
            VerifyAccess();
            _gfx.DrawLine(_pen, (float)x1, (float)y1, (float)x2, (float)y2);
            _cachedGfx.DrawLine(_pen, (float)x1, (float)y1, (float)x2, (float)y2);
        }

        /// <summary>
        /// Draws a line of text on the screen at the specified location.
        /// </summary>
        /// <param name="x">
        /// The x co-ordinate of the text start point
        /// </param>
        /// <param name="y">
        /// The y co-ordinate of the text start point
        /// </param>
        /// <param name="text">
        /// The text to draw
        /// </param>
        public static void DrawText(decimal x, decimal y, string text)
        {
            // Validate
            if (string.IsNullOrEmpty(text))
                return;

            VerifyAccess();
            _gfx.DrawString(text, _font, _brush, (float)x, (float)y);
            _cachedGfx.DrawString(text, _font, _brush, (float)x, (float)y);
        }

        /// <summary>
        /// Draws a line of text on the screen at the specified location.
        /// </summary>
        /// <param name="x">
        /// The x co-ordinate of the text start point
        /// </param>
        /// <param name="y">
        /// The y co-ordinate of the text start point
        /// </param>
        /// <param name="text">
        /// The text to draw
        /// </param>
        /// <param name="width">
        /// The maximum available width.  This parameter helps define when the text should wrap
        /// </param>
        public static void DrawText(decimal x, decimal y, string text, decimal width)
        {
            // Validate
            if (string.IsNullOrEmpty(text))
                return;

            VerifyAccess();
            _gfx.DrawString(text, _font, _brush, new RectangleF((float)x, (float)y, (float)width, 2000));
            _cachedGfx.DrawString(text, _font, _brush, new RectangleF((float)x, (float)y, (float)width, 2000));
        }

        /// <summary>
        /// Draws the specified image from memory on to the screen.  The specified image should first be 
        /// loaded into memory using the ImageList class.
        /// </summary>
        /// <param name="imageName">
        /// The name of the image to draw
        /// </param>
        /// <param name="x">
        /// The x co-ordinate of the point to draw the image at
        /// </param>
        /// <param name="y">
        /// The y co-ordinate of the point to draw the image at
        /// </param>
        public static void DrawImage(string imageName, decimal x, decimal y)
        {
            Bitmap image = ImageList.GetBitmap(imageName);
            if (image != null)
            {
                VerifyAccess();
                _gfx.DrawImage(image, (float)x, (float)y);
                _cachedGfx.DrawImage(image, (float)x, (float)y);
            }
        }

        /// <summary>
        /// Draws the specified image from memory on to the screen.  The specified image should first be 
        /// loaded into memory using the ImageList class.
        /// </summary>
        /// <param name="imageName">
        /// The name of the image to draw
        /// </param>
        /// <param name="x">
        /// The x co-ordinate of the point to draw the image at
        /// </param>
        /// <param name="y">
        /// The y co-ordinate of the point to draw the image at
        /// </param>
        /// <param name="width">
        /// The destination width of the image on the screen
        /// </param>
        /// <param name="height">
        /// The destination height of the image on the screen
        /// </param>
        public static void DrawImage(string imageName, decimal x, decimal y, decimal width, decimal height)
        {
            Bitmap image = ImageList.GetBitmap(imageName);
            if (image != null)
            {
                VerifyAccess();
                _gfx.DrawImage(image, (float)x, (float)y, (float)width, (float)height);
                _cachedGfx.DrawImage(image, (float)x, (float)y, (float)width, (float)height);
            }
        }

        /// <summary>
        /// Sets the Pen Color for drawing
        /// </summary>
        /// <param name="red">
        /// Value between 0 and 255 representing the intensity of Red Color
        /// </param>
        /// <param name="green">
        /// Value between 0 and 255 representing the intensity of Green Color
        /// </param>
        /// <param name="blue">
        /// Value between 0 and 255 representing the intensity of Blue Color
        /// </param>
        public static void SetPenColor(decimal red, decimal green, decimal blue)
        {
            if (_pen != Pens.Black && _pen != null)
                _pen.Dispose();
            _pen = new Pen(Color.FromArgb((int)red % 256, (int)green % 256, (int)blue % 256));
        }

        /// <summary>
        /// Sets the Brush Color for drawing
        /// </summary>
        /// <param name="red">
        /// Value between 0 and 255 representing the intensity of Red Color
        /// </param>
        /// <param name="green">
        /// Value between 0 and 255 representing the intensity of Green Color
        /// </param>
        /// <param name="blue">
        /// Value between 0 and 255 representing the intensity of Blue Color
        /// </param>
        public static void SetBrushColor(decimal red, decimal green, decimal blue)
        {
            if (_brush != Brushes.Coral && _brush != null)
                _brush.Dispose();
            _brush = new SolidBrush(Color.FromArgb((int)red % 256, (int)green % 256, (int)blue % 256));
        }

        /// <summary>
        /// Sets the current font for drawing text.
        /// </summary>
        /// <param name="fontName">
        /// The name of the font's type face
        /// </param>
        /// <param name="size">
        /// The size of the text
        /// </param>
        public static void SetFont(string fontName, decimal size)
        {
            SetFont(fontName, size, false, false, false);
        }

        /// <summary>
        /// Sets the current font for drawing text.
        /// </summary>
        /// <param name="fontName">
        /// The name of the font's type face
        /// </param>
        /// <param name="size">
        /// The size of the text
        /// </param>
        /// <param name="bold">
        /// Specifies whether or not the text is to be drawn in bold form
        /// </param>
        /// <param name="italics">
        /// Specifies whether or not the text is to be drawn in italics 
        /// </param>
        /// <param name="underline">
        /// Specifies whether or not the text is to be drawn underlined
        /// </param>
        public static void SetFont(string fontName, decimal size, bool bold, bool italics, bool underline)
        {
            // Validate
            if (fontName == null || size < 0)
                return;

            if (_font != null)
                _font.Dispose();
            FontStyle style = FontStyle.Regular;
            if (bold)
                style = style | FontStyle.Bold;
            if (italics)
                style = style | FontStyle.Italic;
            if (underline)
                style = style | FontStyle.Underline;
            _font = new Font(fontName, (float)size, style);
        }

        /// <summary>
        /// Clears the window
        /// </summary>
        public static void Clear()
        {
            _gfx.FillRectangle(Brushes.White, 0, 0, (float)Width, (float)Height);
            _cachedGfx.FillRectangle(Brushes.White, 0, 0, (float)Width, (float)Height);
        }

        /// <summary>
        /// Saves the current page of the window into memory using the specified name
        /// </summary>
        /// <param name="name">
        /// The name for the saved page
        /// </param>
        public static void SavePageToMemory(string name)
        {
            // Copy over the cached image to the save bitmap
            Bitmap saveImage = new Bitmap((int)Width, (int)Height);
            Graphics saveGfx = Graphics.FromImage(saveImage);
            saveGfx.DrawImage(_currentPage, new Point(0, 0));
            saveGfx.Dispose();

            _savedPages[name] = saveImage;
        }

        /// <summary>
        /// Restores a saved page from memory into the window
        /// </summary>
        /// <param name="name">
        /// The name for the saved page
        /// </param>
        public static void RestorePageFromMemory(string name)
        {
            Bitmap saveImage = null;
            if (_savedPages.TryGetValue(name, out saveImage))
            {
                _gfx.DrawImage(saveImage, new Point(0, 0));
                _cachedGfx.DrawImage(saveImage, new Point(0, 0));
            }
        }

        /// <summary>
        /// Waits until the graphics window is closed
        /// </summary>
        public static void WaitForClose()
        {
            AutoResetEvent closeEvent = new AutoResetEvent(false);
            FormClosedEventHandler closeEventHandler = null;
            closeEventHandler = delegate
            {
                _window.FormClosed -= closeEventHandler;
                closeEvent.Set();
            };
            _window.FormClosed += closeEventHandler;
            closeEvent.WaitOne();
        }

        /// <summary>
        /// Gets or sets the title for the graphics window
        /// </summary>
        public static string Title
        {
            get
            {
                VerifyAccess();
                string title = "";
                _window.Invoke((EventHandler)delegate { title = _window.Text; });
                return title;
            }

            set
            {
                VerifyAccess();
                _window.Invoke((EventHandler)delegate { _window.Text = value; });
            }
        }

        /// <summary>
        /// Gets or sets the Height of the graphics window
        /// </summary>
        public static decimal Height
        {
            get
            {
                VerifyAccess();
                int height = 0;
                _window.Invoke((EventHandler)delegate { height = _window.Height; });
                return height;
            }

            set
            {
                VerifyAccess();
                _window.Invoke((EventHandler)delegate { _window.Height = (int)value; });
            }
        }

        /// <summary>
        /// Gets or sets the Width of the graphics window
        /// </summary>
        public static decimal Width
        {
            get
            {
                VerifyAccess();
                int width = 0;
                _window.Invoke((EventHandler)delegate { width = _window.Width; });
                return width;
            }

            set
            {
                VerifyAccess();
                _window.Invoke((EventHandler)delegate { _window.Width = (int)value; });
            }
        }

        /// <summary>
        /// Gets the x-position of the mouse relative to the Graphics Window
        /// </summary>
        public static decimal MouseX
        {
            get
            {
                return _mouseX;
            }
        }

        /// <summary>
        /// Gets the y-position of the mouse relative to the Graphics Window
        /// </summary>
        public static decimal MouseY
        {
            get
            {
                return _mouseY;
            }
        }

        /// <summary>
        /// Raises an event when the mouse button is clicked down
        /// </summary>
        public static event SmallBasicCallback MouseDown
        {
            add
            {
                _mouseDown = value;
            }

            remove
            {
                _mouseDown -= value;
            }
        }

        /// <summary>
        /// Raises an event when the mouse button is released
        /// </summary>
        public static event SmallBasicCallback MouseUp
        {
            add
            {
                _mouseUp = value;
            }

            remove
            {
                _mouseUp -= value;
            }
        }

        /// <summary>
        /// Raises an event when the mouse is moved around
        /// </summary>
        public static event SmallBasicCallback MouseMove
        {
            add
            {
                _mouseMove = value;
            }

            remove
            {
                _mouseMove -= value;
            }
        }

        #region Private Helpers

        /// <summary>
        /// Creates the graphics window &amp; the thread associated with it
        /// </summary>
        static void CreateWindow()
        {
            AutoResetEvent waitEvent = new AutoResetEvent(false);
            _windowThread = new Thread((ThreadStart)delegate
            {
                _window = new Form();
                _windowCreated = true;

                _window.BackColor = Color.White;
                _window.MaximizeBox = false;
                _window.MinimizeBox = false;
                
                waitEvent.Set();
                NativeHelper.SetForegroundWindow(_window.Handle);
                Application.Run(_window);
            });

            _windowThread.SetApartmentState(ApartmentState.STA);
            _windowThread.Start();
            waitEvent.WaitOne();

            _currentPage = new Bitmap(_imageWidth, _imageHeight);
            _cachedGfx = Graphics.FromImage(_currentPage);
            _cachedGfx.FillRectangle(Brushes.White, 0, 0, _imageWidth, _imageHeight);
            _gfx = _window.CreateGraphics();

            SetDCProperties(_gfx);
            SetDCProperties(_cachedGfx);

            _window.SizeChanged += new EventHandler(WindowSizeChanged);
            _window.Paint += new PaintEventHandler(WindowPaint);
            _window.FormClosing += new FormClosingEventHandler(WindowClosing);
            if (_pen == null)
                _pen = Pens.Black;
            if (_brush == null)
                _brush = Brushes.Coral;
            _font = new Font("Tahoma", 12);

            // Sign up for events
            _window.MouseDown += delegate { if (_mouseDown != null) _mouseDown(); };
            _window.MouseUp += delegate { if (_mouseUp != null) _mouseUp(); };
            _window.MouseMove += delegate(object sender, MouseEventArgs e)
            {
                _mouseX = e.X;
                _mouseY = e.Y;
                if (_mouseMove != null) 
                    _mouseMove();
            };
        }

        /// <summary>
        /// When the user closes the window - cancel that and hide the window
        /// </summary>
        static void WindowClosing(object sender, FormClosingEventArgs e)
        {
            //e.Cancel = true;
            //Hide();
        }

        /// <summary>
        /// Handles the paint window message and draws the cached bitmap on it
        /// </summary>
        static void WindowPaint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(_currentPage, new Point(0, 0));
        }

        /// <summary>
        /// Handles the Window Size change event and resizes the background image as needed
        /// </summary>
        static void WindowSizeChanged(object sender, EventArgs e)
        {
            int width = _currentPage.Width;
            int height = _currentPage.Height;
            if (_currentPage.Width < _window.Width)
                width = _window.Width + _step;
            else if (_currentPage.Height < _window.Height)
                height = _window.Height + _step;
            else
                return; // No updates necessary

            // We need to update the bitmap - but not lose the original drawing
            _cachedGfx.Dispose();
            _gfx.Dispose();

            Bitmap oldBitmap = _currentPage;
            _currentPage = new Bitmap(width, height);
            
            // Update the new device context and bitmap
            _gfx = _window.CreateGraphics();
            _cachedGfx = Graphics.FromImage(_currentPage);
            _cachedGfx.FillRectangle(Brushes.White, 0, 0, width, height);
            _cachedGfx.DrawImage(oldBitmap, new Point(0, 0));

            // Set the graphics properties
            SetDCProperties(_gfx);
            SetDCProperties(_cachedGfx);

            oldBitmap.Dispose();
        }

        /// <summary>
        /// Verifies if the access to Console Window has been made yet
        /// </summary>
        static void VerifyAccess()
        {
            // Show the window if it's not visible yet
            if (!_windowCreated)
                Show();
        }

        /// <summary>
        /// Sets the DC properties
        /// </summary>
        /// <param name="gfx">
        /// The DC
        /// </param>
        static void SetDCProperties(Graphics gfx)
        {
            gfx.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
            gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
        }

        #endregion // Private Helpers
    }
}