// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.StaticFiles
{
    /// <summary>
    /// Defines settings used for caching responses for the <see cref="StaticFileMiddleware"/>.
    /// </summary>
    public class CacheProfile
    {
        /// <summary>
        /// Gets or sets the duration for which the response is cached.
        /// Sets the "max-age" in the "Cache-control" header in the 
        /// <see cref="Microsoft.AspNetCore.Http.HttpContext.Response" />.
        /// Defaults to 10 minutes
        /// </summary>
        public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Gets or sets the location where the data from a particular URL must be cached.
        /// Sets the "Cache-control" header in the 
        /// <see cref="Microsoft.AspNetCore.Http.HttpContext.Response" />. 
        /// Defaults to  <see cref="ResponseCacheLocation.Any" />
        /// </summary>
        public ResponseCacheLocation Location { get; set; } = ResponseCacheLocation.Any;

        /// <summary>
        /// Gets or sets the value which determines whether the data should be stored or not.
        /// When set to <see langword="true"/>, it sets "Cache-control" header in
        /// <see cref="Microsoft.AspNetCore.Http.HttpContext.Response" /> to "no-store".
        /// Ignores the "Location" parameter for values other than "None".
        /// Ignores the "Duration" parameter.
        /// Defaults to <see langword="false"/>.
        /// </summary>
        public bool NoStore { get; set; } = false;

        /// <summary>
        /// Gets or sets the value for the Vary header in <see cref="Microsoft.AspNetCore.Http.HttpContext.Response" />.
        /// </summary>
        public string VaryByHeader { get; set; }
    }
}