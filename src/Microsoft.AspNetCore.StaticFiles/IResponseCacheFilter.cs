using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.StaticFiles
{
    internal interface IResponseCacheFilter
    {
        /// <summary>
        /// Apply the given cacheProfile to the <see cref="HttpContext"/>
        /// </summary>
        /// <param name="context">The current <see cref="HttpContext"/></param>
        /// <param name="cacheProfile">The profile which contains the cache control settings</param>
        void ApplyCacheProfile(HttpContext context, CacheProfile cacheProfile);
    }
}