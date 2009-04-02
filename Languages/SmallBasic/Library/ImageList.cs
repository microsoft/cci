// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.SmallBasic.Library
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;

    /// <summary>
    /// This class helps to load and store images in memory.
    /// </summary>
    public static class ImageList
    {
        #region Private Fields

        static Dictionary<string, Bitmap> _savedImages = new Dictionary<string, Bitmap>();

        #endregion // Private Fields

        /// <summary>
        /// Loads an image from a file or the internet into memory.
        /// </summary>
        /// <param name="imageName">
        /// The name of the image in memory
        /// </param>
        /// <param name="fileNameOrUrl">
        /// The file name to load the image from.  This could be a local file or a url to the internet location
        /// </param>
        public static void LoadImage(string imageName, string fileNameOrUrl)
        {
            // Validate
            if (string.IsNullOrEmpty(imageName) || string.IsNullOrEmpty(fileNameOrUrl))
                return;

            string localFileName = NetworkHelper.GetLocalFile(fileNameOrUrl);
            try
            {
                Bitmap fileImage = new Bitmap(localFileName);
                Bitmap memoryImage = new Bitmap(fileImage.Width, fileImage.Height);
                Graphics gfx = Graphics.FromImage(memoryImage);
                gfx.DrawImage(fileImage, 0, 0, fileImage.Width, fileImage.Height);
                _savedImages[imageName] = memoryImage;

                gfx.Dispose();
                fileImage.Dispose();
                
                // Now, if the file was downloaded, go ahead and delete it
                if (String.Compare(fileNameOrUrl, localFileName, true) != 0)
                    System.IO.File.Delete(localFileName);
            }
            catch (Exception e)
            {
                // This could be anything from file not found to file format problems
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// Gets the width of the stored image.
        /// </summary>
        /// <param name="imageName">
        /// The name of the image in memory
        /// </param>
        /// <returns>
        /// The width of the specified image
        /// </returns>
        public static int GetWidthOfImage(string imageName)
        {
            Bitmap image = GetBitmap(imageName);
            if (image == null)
                return 0;
            else
                return image.Width;
        }

        /// <summary>
        /// Gets the height of the stored image.
        /// </summary>
        /// <param name="imageName">
        /// The name of the image in memory
        /// </param>
        /// <returns>
        /// The height of the specified image
        /// </returns>
        public static int GetHeightOfImage(string imageName)
        {
            Bitmap image = GetBitmap(imageName);
            if (image == null)
                return 0;
            else
                return image.Height;
        }

        #region Internal Helpers

        /// <summary>
        /// Gets a loaded bitmap from memory given the image name.
        /// </summary>
        internal static Bitmap GetBitmap(string imageName)
        {
            Bitmap image = null;
            if (!string.IsNullOrEmpty(imageName))
                _savedImages.TryGetValue(imageName, out image);
            return image;
        }

        /// <summary>
        /// Converts the specified image to Bitmap format and returns the new file name.
        /// </summary>
        internal static string ConvertToBitmap(string fileName)
        {
            // Validate
            if (string.IsNullOrEmpty(fileName))
                return null;

            string localFile = Path.GetTempFileName();
            Bitmap image = new Bitmap(fileName);
            image.Save(localFile, System.Drawing.Imaging.ImageFormat.Bmp);
            image.Dispose();

            return localFile;
        }

        #endregion // Internal Helpers
    }
}