// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.StaticFiles.Tests
{
    public class ResponseCacheFilterTests
    {
        public static IEnumerable<object[]> CacheControlData
        {
            get
            {
                yield return new object[] {
                    new CacheProfile
                    {
                        Duration = 0, Location = ResponseCacheLocation.Any, NoStore = true, VaryByHeader = null
                    },
                    "no-store"
                };
                // If no-store is set, then location is ignored.
                yield return new object[] {
                    new CacheProfile
                    {
                        Duration = 0, Location = ResponseCacheLocation.Client, NoStore = true, VaryByHeader = null
                    },
                    "no-store"
                };
                yield return new object[] {
                    new CacheProfile
                    {
                        Duration = 0, Location = ResponseCacheLocation.Any, NoStore = true, VaryByHeader = null
                    },
                    "no-store"
                };
                // If no-store is set, then duration is ignored.
                yield return new object[] {
                    new CacheProfile
                    {
                        Duration = 100, Location = ResponseCacheLocation.Any, NoStore = true, VaryByHeader = null
                    },
                    "no-store"
                };

                yield return new object[] {
                    new CacheProfile
                    {
                        Duration = 10, Location = ResponseCacheLocation.Client,
                        NoStore = false, VaryByHeader = null
                    },
                    "private,max-age=10"
                };
                yield return new object[] {
                    new CacheProfile
                    {
                        Duration = 10, Location = ResponseCacheLocation.Any, NoStore = false, VaryByHeader = null
                    },
                    "public,max-age=10"
                };
                yield return new object[] {
                    new CacheProfile
                    {
                        Duration = 10, Location = ResponseCacheLocation.None, NoStore = false, VaryByHeader = null
                    },
                    "no-cache,max-age=10"
                };
                yield return new object[] {
                    new CacheProfile
                    {
                        Duration = 31536000, Location = ResponseCacheLocation.Any,
                        NoStore = false, VaryByHeader = null
                    },
                    "public,max-age=31536000"
                };
                yield return new object[] {
                    new CacheProfile
                    {
                        Duration = 20, Location = ResponseCacheLocation.Any, NoStore = false, VaryByHeader = null
                    },
                    "public,max-age=20"
                };
            }
        }

        [Theory]
        [MemberData(nameof(CacheControlData))]
        public void ApplyCacheProfile_CanSetCacheControlHeaders(CacheProfile cacheProfile, string output)
        {
            // Arrange
            var responseCacheFilter = new ResponseCacheFilter();
            var context = new DefaultHttpContext();

            // Act
            responseCacheFilter.ApplyCacheProfile(context, cacheProfile);

            // Assert
            Assert.Equal(output, context.Response.Headers["Cache-control"]);
        }

        public static IEnumerable<object[]> NoStoreData
        {
            get
            {
                // If no-store is set, then location is ignored.
                yield return new object[] {
                    new CacheProfile
                    {
                        Duration = 0, Location = ResponseCacheLocation.Client, NoStore = true, VaryByHeader = null
                    },
                    "no-store"
                };
                yield return new object[] {
                    new CacheProfile
                    {
                        Duration = 0, Location = ResponseCacheLocation.Any, NoStore = true, VaryByHeader = null
                    },
                    "no-store"
                };
                // If no-store is set, then duration is ignored.
                yield return new object[] {
                    new CacheProfile
                    {
                        Duration = 100, Location = ResponseCacheLocation.Any, NoStore = true, VaryByHeader = null
                    },
                    "no-store"
                };
            }
        }

        [Theory]
        [MemberData(nameof(NoStoreData))]
        public void ApplyCacheProfile_DoesNotSetLocationOrDuration_IfNoStoreIsSet(
            CacheProfile cacheProfile, string output)
        {
            // Arrange
            var responseCacheFilter = new ResponseCacheFilter();
            var context = new DefaultHttpContext();

            // Act
            responseCacheFilter.ApplyCacheProfile(context, cacheProfile);

            // Assert
            Assert.Equal(output, context.Response.Headers["Cache-control"]);
        }

        public static IEnumerable<object[]> VaryData
        {
            get
            {
                yield return new object[] {
                    new CacheProfile
                    {
                        Duration = 10, Location = ResponseCacheLocation.Any,
                        NoStore = false, VaryByHeader = "Accept"
                    },
                    "Accept",
                    "public,max-age=10" };
                yield return new object[] {
                    new CacheProfile
                    {
                        Duration = 0, Location= ResponseCacheLocation.Any,
                        NoStore = true, VaryByHeader = "Accept"
                    },
                    "Accept",
                    "no-store"
                };
                yield return new object[] {
                    new CacheProfile
                    {
                        Duration = 10, Location = ResponseCacheLocation.Client,
                        NoStore = false, VaryByHeader = "Accept"
                    },
                    "Accept",
                    "private,max-age=10"
                };
                yield return new object[] {
                    new CacheProfile
                    {
                        Duration = 10, Location = ResponseCacheLocation.Client,
                        NoStore = false, VaryByHeader = "Test"
                    },
                    "Test",
                    "private,max-age=10"
                };
                yield return new object[] {
                    new CacheProfile
                    {
                        Duration = 31536000, Location = ResponseCacheLocation.Any,
                        NoStore = false, VaryByHeader = "Test"
                    },
                    "Test",
                    "public,max-age=31536000"
                };
            }
        }

        [Theory]
        [MemberData(nameof(VaryData))]
        public void ResponseCacheCanSetVary(CacheProfile cacheProfile, string varyOutput, string cacheControlOutput)
        {
            // Arrange
            var responseCacheFilter = new ResponseCacheFilter();
            var context = new DefaultHttpContext();

            // Act
            responseCacheFilter.ApplyCacheProfile(context, cacheProfile);

            // Assert
            Assert.Equal(varyOutput, context.Response.Headers["Vary"]);
            Assert.Equal(cacheControlOutput, context.Response.Headers["Cache-control"]);
        }

        [Fact]
        public void SetsPragmaOnNoCache()
        {
            // Arrange
            var responseCacheFilter = new ResponseCacheFilter();
            var cacheProfile = new CacheProfile
                {
                    Duration = 0,
                    Location = ResponseCacheLocation.None,
                    NoStore = true,
                    VaryByHeader = null
                };
            var context = new DefaultHttpContext();

            // Act
            responseCacheFilter.ApplyCacheProfile(context, cacheProfile);

            // Assert
            Assert.Equal("no-store,no-cache", context.Response.Headers["Cache-control"]);
            Assert.Equal("no-cache", context.Response.Headers["Pragma"]);
        }
    }
}
