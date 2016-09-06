﻿namespace Microsoft.AspNetCore.StaticFiles
{
    /// <summary>
    /// Determines the value for the "Cache-control" header in the response.
    /// </summary>
    public enum ResponseCacheLocation
    {
        /// <summary>
        /// Cached in both proxies and client.
        /// Sets "Cache-control" header to "public".
        /// </summary>
        Any = 0,
        /// <summary>
        /// Cached only in the client.
        /// Sets "Cache-control" header to "private".
        /// </summary>
        Client = 1,
        /// <summary>
        /// "Cache-control" and "Pragma" headers are set to "no-cache".
        /// </summary>
        None = 2
    }
}