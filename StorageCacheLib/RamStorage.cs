//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace StorageCacheLib
//{
//    class RamStorage : ISiteCacheStorage
//    {
//        #region Properties
//        public long OccupancyInBytes { get => _occupancyInBytes; }
//        #endregion


//        #region Private Fields
//        private long _occupancyInBytes;
//        private Dictionary<string, byte[]> _contentStore;
//        #endregion

//        #region Constructor(s)
//        public RamStorage()
//        {
//            _contentStore = new Dictionary<string, byte[]>();
//        }
//        #endregion


//        #region Public Interface Methods
//        public long GetLocalSize(string siteRoot)
//        {
//            if (_contentStore.ContainsKey(siteRoot))
//            {
//                return _contentStore[siteRoot].LongLength;
//            }
//            else
//            {
//                return -1L;
//            }

//        }

//        /// <summary>
//        /// Adds a site's current zip to the storage
//        /// </summary>
//        /// <param name="siteRoot"></param>
//        /// <param name="content">The buffered form of the site's content (or null if it should be fetched from origin)</param>
//        /// <returns>Size of the local copy (B)</returns>
//        public long AddSite(string siteRoot, byte[] content = null)
//        {
//            // Get the content of the site to store
//            byte[] storageContent;
//            if (content == null)
//            {
//                // Need to actually fetch from the origin
//                string originFilepath = ContentFileHelper.GetCurrentSiteZipFilepath(siteRoot);
//                storageContent = File.ReadAllBytes(originFilepath);
//            }
//            else
//            {
//                // Provided the content to store (Make a copy that we can control)
//                storageContent = new byte[content.Length];
//                Buffer.BlockCopy(content, 0, storageContent, 0, content.Length);
//            }

//            // Store the content
//            _contentStore.Add(siteRoot, storageContent);

//            // Update the occupancy
//            _occupancyInBytes += storageContent.LongLength;

//            // Done
//            return storageContent.LongLength;
//        }

//        /// <summary>
//        /// Updates a site's local zip copy instance based on the current one designated in the site root
//        /// </summary>
//        /// <param name="siteRoot"></param>
//        /// <param name="content">The buffered form of the newest verson of the site's content (or null if it should be fetched from origin)</param>
//        /// <returns>The delta in occupied capacity (B)</returns>
//        public long UpdateSite(string siteRoot, byte[] content = null)
//        {
//            // Delete the old one if present
//            long oldCopySize = DeleteSite(siteRoot);

//            // Get a local copy of the newer file
//            long newCopySize = AddSite(siteRoot, content);

//            return newCopySize - oldCopySize;
//        }

//        /// <summary>
//        /// Retrieves a byte array with the content from from the site
//        /// </summary>
//        /// <param name="siteTag">The ContentCache SiteTag for the desired site</param>
//        /// <param name="startingOffset">The offset within the site's zip at which to start</param>
//        /// <param name="lengthBytes">The length of requested sequential chunk, or negative value for rest of the file after the starting offset</param>
//        /// <returns></returns>
//        public byte[] GetSiteContent(string siteRoot, int startingOffset = 0, int lengthBytes = -1)
//        {
//            byte[] currentContent;
//            if (_contentStore.ContainsKey(siteRoot))
//            {
//                currentContent = _contentStore[siteRoot];
//            }
//            else
//            {
//                currentContent = null;
//            }

//            // Done
//            return currentContent;
//        }

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="siteTag"></param>
//        /// <returns>Amount of space freed (MB) (0 if was not present)</returns>
//        public long DeleteSite(string siteRoot)
//        {
//            if (_contentStore.ContainsKey(siteRoot))
//            {
//                // Get the content size info
//                long siteContentSize = _contentStore[siteRoot].LongLength;

//                // Need to remove from the storage
//                _contentStore.Remove(siteRoot);

//                _occupancyInBytes -= siteContentSize;
//                return siteContentSize;
//            }
//            else
//            {
//                return 0;
//            }
//        }

//        /// <summary>
//        /// Clears the contents and all internal state for the cache storage
//        /// </summary>
//        public void Clear()
//        {
//            _contentStore.Clear();
//            _occupancyInBytes = 0L;
//        }
//        #endregion
//    }
//}
