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

        // Return Site's zip content if SiteRoot exists and contains a Run From Zip file in (RootDirectory\data\SitePacakges)
        byte[] AddSite(string siteRoot, string storageVolume, CachedContentType contentType = CachedContentType.Zip);

        byte[] AddSiteWithUrl(string siteName, string urlToDownloadFrom, CachedContentType contentType = CachedContentType.Zip);

        // Returns latest zip content for the site if SiteRoot exists and contains a Run From Zip file in (RootDirectory\data\SitePacakges)
        byte[] RefreshSiteContents(string siteName, string remoteContentPath, CachedContentType contentType = CachedContentType.Zip);

        byte[] GetSiteContents(string siteName, string remoteContentPath, CachedContentType contentType = CachedContentType.Zip);

        // Deletes Site from Cache
        void DeleteSite(string siteRoot);

        // Empties Cache directory and resets all state
        void ClearCache();
    }
}
