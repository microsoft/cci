// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.SmallBasic.Library
{
    using System;
    using System.IO;
    using System.Net;

    /// <summary>
    /// This private helper class provides network access methods
    /// </summary>
    static class NetworkHelper
    {
        /// <summary>
        /// Downloads a file from the network to a local temporary file
        /// </summary>
        /// <param name="url">
        /// The url of the file on the network
        /// </param>
        /// <returns>
        /// A local file name that the remote file was downloaded as
        /// </returns>
        public static string DownloadFile(string url)
        {
            // Validate
            if (string.IsNullOrEmpty(url))
                return null;

            string localFile = Path.GetTempFileName();
            Stream writer = null;
            Stream reader = null;
            WebResponse response = null;

            try
            {
                WebRequest request = WebRequest.Create(url);
                response = request.GetResponse();
                writer = File.Open(localFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);

                // Now, read and write to the local file
                byte[] buffer = new byte[4096];
                long length = response.ContentLength;
                reader = response.GetResponseStream();
                while (length > 0)
                {
                    int readLength = reader.Read(buffer, 0, 4096);
                    writer.Write(buffer, 0, readLength);
                    length -= readLength;
                }
            }
            catch(Exception e)
            {
                // This could be a variety of exceptions...
                System.Diagnostics.Debug.WriteLine(e.ToString());
                return null;
            }
            finally
            {
                if (writer != null)
                    writer.Close();
                if (reader != null)
                    reader.Close();
                if (response != null)
                    response.Close();
            }

            return localFile;
        }

        /// <summary>
        /// Gets the contents of a specified web page.
        /// </summary>
        /// <param name="url">
        /// The url of the web page
        /// </param>
        /// <returns>
        /// The contents of the specified web page
        /// </returns>
        public static string GetWebPageContents(string url)
        {
            // Validate
            if (string.IsNullOrEmpty(url))
                return null;

            StreamReader reader = null;
            WebResponse response = null;
            string contents = "";

            try
            {
                WebRequest request = WebRequest.Create(url);
                response = request.GetResponse();
                
                reader = new StreamReader(response.GetResponseStream());
                contents = reader.ReadToEnd();
            }
            catch (Exception e)
            {
                // This could be a variety of exceptions...
                System.Diagnostics.Debug.WriteLine(e.ToString());
                return null;
            }
            finally
            {
                if (reader != null)
                    reader.Close();
                if (response != null)
                    response.Close();
            }

            return contents;
        }

        /// <summary>
        /// Gets a local copy of the specified file.  If it's already local, just returns it, else downloads
        /// the file to a local copy and returns it.
        /// </summary>
        internal static string GetLocalFile(string fileNameOrUrl)
        {
            // Validate
            if (string.IsNullOrEmpty(fileNameOrUrl))
                return null;

            Uri fileUri = new Uri(fileNameOrUrl);
            if (fileUri.IsFile)
                return fileNameOrUrl;
            else
                return DownloadFile(fileNameOrUrl);
        }
    }
}