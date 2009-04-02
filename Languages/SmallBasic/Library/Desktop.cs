// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.SmallBasic.Library
{
    using System;
    using System.Drawing;

    /// <summary>
    /// This class provides methods to interact with the desktop
    /// </summary>
    public static class Desktop
    {
        /// <summary>
        /// Sets the specified picture as the desktop's wallpaper.  This file could be a local file or a 
        /// network file or even an internet url.
        /// </summary>
        /// <param name="fileOrUrl">
        /// The filename or url of the picture
        /// </param>
        public static void SetWallPaper(string fileOrUrl)
        {
            // Validate
            if (string.IsNullOrEmpty(fileOrUrl))
                return;

            string localFile = NetworkHelper.GetLocalFile(fileOrUrl);
            string bitmapFile = ImageList.ConvertToBitmap(localFile);
            NativeHelper.SystemParametersInfo(NativeHelper.SPI_SETDESKWALLPAPER, 0, bitmapFile, NativeHelper.SPIF_UPDATEINIFILE | NativeHelper.SPIF_SENDWININICHANGE);
        }
    }
}