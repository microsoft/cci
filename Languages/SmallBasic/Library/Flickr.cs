// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.SmallBasic.Library
{
    using System;
    using System.Xml;
    
    /// <summary>
    /// This class provides access to Flickr photo services
    /// </summary>
    public static class Flickr
    {
        const string _urlTemplate = "http://www.flickr.com/services/feeds/photos_public.gne?format=rss";

        /// <summary>
        /// Gets the url for the picture of the moment.
        /// </summary>
        /// <returns>
        /// A file url for Flickr's picture of the moment
        /// </returns>
        public static string GetPictureOfMomentUrl()
        {
            return GetPictureUrl(_urlTemplate);
        }

        /// <summary>
        /// Gets the url for a random scenic picture.
        /// </summary>
        /// <returns>
        /// A file url for Flickr's random scenic picture
        /// </returns>
        public static string GetRandomScenicPicture()
        {
            return GetPictureUrl(_urlTemplate + "&tags=nature");
        }

        /// <summary>
        /// Gets the url for a random picture tagged with the specified tag.
        /// </summary>
        /// <returns>
        /// A file url for Flickr's random picture
        /// </returns>
        public static string GetRandomPicture(string tag)
        {
            return GetPictureUrl(_urlTemplate + string.Format("&tags={0}", tag));
        }

        
        /// <summary>
        /// A private helper to get the first image in an rss feed from Flickr
        /// </summary>
        static string GetPictureUrl(string feedUrl)
        {
            string url = "";

            // Get the rss contents and grok it to find the first picture of the moment
            string rssContents = NetworkHelper.GetWebPageContents(feedUrl);

            try
            {
                XmlDocument document = new XmlDocument();
                document.LoadXml(rssContents);

                // Now, get hold of the first item node
                XmlNode itemNode = document.SelectSingleNode("/rss/channel/item/description");
                string textToParse = itemNode.InnerText.ToLowerInvariant();

                // Look for the img tag
                int imgIndex = textToParse.IndexOf("<img src=\"");
                if (imgIndex != -1)
                {
                    int endQuoteIndex = textToParse.IndexOf("\" ", imgIndex + 10);
                    if (endQuoteIndex != -1)
                    {
                        string thumbNailUrl = textToParse.Substring(imgIndex + 10, endQuoteIndex - imgIndex - 10);
                        url = thumbNailUrl.Replace("_m.", "_o.");
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }

            return url;
        }
        //
        //a_o_d.jpg
    }
}