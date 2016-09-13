// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.StaticFiles
{
    internal class ResponseCacheFilter : IResponseCacheFilter
    {
        /// <summary>
        /// Apply the given cacheProfile to the <see cref="HttpContext"/>
        /// </summary>
        /// <param name="context">The current <see cref="HttpContext"/></param>
        /// <param name="cacheProfile">The profile which contains the cache control settings</param>
        public void ApplyCacheProfile(HttpContext context, CacheProfile cacheProfile)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (cacheProfile == null)
            {
                throw new ArgumentNullException(nameof(cacheProfile));
            }

            var noStore = cacheProfile.NoStore ?? false;
            var varyByHeader = cacheProfile.VaryByHeader;
            var location = cacheProfile.Location ?? ResponseCacheLocation.Any;

            if (!noStore && cacheProfile.Duration == null)
            {
                // Duration MUST be set unless NoStore is true.
                throw new InvalidOperationException(
                        Resources.FormatResponseCache_SpecifyDuration(
                            nameof(CacheProfile.NoStore), nameof(CacheProfile.Duration)));

            }

            var headers = context.Response.Headers;

            // Clear all headers
            headers.Remove(HeaderNames.Vary);
            headers.Remove(HeaderNames.CacheControl);
            headers.Remove(HeaderNames.Pragma);

            if (!string.IsNullOrEmpty(varyByHeader))
            {
                headers[HeaderNames.Vary] = varyByHeader;
            }

            if (noStore)
            {
                headers[HeaderNames.CacheControl] = "no-store";

                // Cache-control: no-store, no-cache is valid.
                if (location == ResponseCacheLocation.None)
                {
                    headers.AppendCommaSeparatedValues(HeaderNames.CacheControl, "no-cache");
                    headers[HeaderNames.Pragma] = "no-cache";
                }
            }
            else
            {
                string cacheControlValue = null;
                switch (location)
                {
                    case ResponseCacheLocation.Any:
                        cacheControlValue = "public";
                        break;
                    case ResponseCacheLocation.Client:
                        cacheControlValue = "private";
                        break;
                    case ResponseCacheLocation.None:
                        cacheControlValue = "no-cache";
                        headers[HeaderNames.Pragma] = "no-cache";
                        break;
                }

                cacheControlValue = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}{1}max-age={2}",
                    cacheControlValue,
                    cacheControlValue != null ? "," : null,
                    cacheProfile.Duration);

                if (cacheControlValue != null)
                {
                    headers[HeaderNames.CacheControl] = cacheControlValue;
                }
            }
        }
    }
}
