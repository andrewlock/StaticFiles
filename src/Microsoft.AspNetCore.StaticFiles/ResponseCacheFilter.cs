// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            var headers = context.Response.Headers;

            if (!string.IsNullOrEmpty(cacheProfile.VaryByHeader))
            {
                headers[HeaderNames.Vary] = cacheProfile.VaryByHeader;
            }

            if (cacheProfile.NoStore)
            {
                headers[HeaderNames.CacheControl] = "no-store";

                // Cache-control: no-store, no-cache is valid.
                if (cacheProfile.Location == ResponseCacheLocation.None)
                {
                    headers.AppendCommaSeparatedValues(HeaderNames.CacheControl, "no-cache");
                    headers[HeaderNames.Pragma] = "no-cache";
                }
            }
            else
            {
                string cacheControlValue = null;
                switch (cacheProfile.Location)
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

                    default:
                        var exception = new NotImplementedException($"Unknown {nameof(ResponseCacheLocation)}: {cacheProfile.Location}");
                        Debug.Fail(exception.ToString());
                        throw exception;
                }

                cacheControlValue = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0},max-age={1}",
                    cacheControlValue,
                    cacheProfile.Duration);

                headers[HeaderNames.CacheControl] = cacheControlValue;
            }
        }
    }
}
