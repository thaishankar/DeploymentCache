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

        // Return Site's zip content if SiteRoot exists and contains a Run From Zip file in (RootDirectory\data\SitePacakges)
        byte[] AddSite(string siteRoot, string storageVolume);

        // Returns latest zip content for the site if SiteRoot exists and contains a Run From Zip file in (RootDirectory\data\SitePacakges)
        byte[] UpdateSite(string siteRoot, string storageVolume);

        byte[] GetSiteContent(string siteRoot, int startingOffset = 0, int lengthBytes = -1);

        // Deletes Site from Cache
        long DeleteSite(string siteRoot);

        // Empties Cache directory and resets all state
        void ClearCache();
    }
}
