using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageCacheLib
{
    internal interface ISiteCacheStorage
    {
        long OccupancyInBytes { get; }

        long GetCacheSizeForSite(string siteRoot);

        /// <summary>
        /// Adds a site's current zip to the storage
        /// </summary>
        /// <param name="siteRoot"></param>
        /// <param name="content">The buffered form of the site's content (or null if it should be fetched from origin)</param>
        /// <returns>Size of the local copy (B)</returns>
        long AddSite(string siteRoot, string storageVolume);

        /// <summary>
        /// Updates a site's local zip copy instance based on the current one designated in the site root
        /// </summary>
        /// <param name="siteRoot"></param>
        /// <param name="content">The buffered form of the newest verson of the site's content (or null if it should be fetched from origin)</param>
        /// <returns>The delta in occupied capacity (B)</returns>
        long UpdateSite(string siteRoot, string storageVolume);

        /// <summary>
        /// Retrieves a byte array with the content from from the site
        /// </summary>
        /// <param name="siteRoot">The fullpath to the site root</param>
        /// <param name="startingOffset">The offset within the site's zip at which to start</param>
        /// <param name="lengthBytes">The length of requested sequential chunk, or negative value for rest of the file after the starting offset</param>
        /// <returns></returns>
        byte[] GetSiteContent(string siteRoot, int startingOffset = 0, int lengthBytes = -1);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="siteRoot"></param>
        /// <returns>Amount of space freed (MB)</returns>
        long DeleteSite(string siteRoot);

        /// <summary>
        /// Clears the contents and all internal state for the cache storage
        /// </summary>
        void ClearCache();
    }
}
