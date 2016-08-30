// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.StaticFiles
{
    public class StaticFileMiddlewareTests
    {
        [Fact]
        public async Task ReturnsNotFoundWithoutWwwroot()
        {
            var baseAddress = "http://localhost:12345";
            var builder = new WebHostBuilder()
                .UseKestrel()
                .Configure(app => app.UseStaticFiles());

            using (var server = builder.Start(baseAddress))
            {
                using (var client = new HttpClient() { BaseAddress = new Uri(baseAddress) })
                {
                    var response = await client.GetAsync("TestDocument.txt");

                    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task FoundFile_LastModifiedTrimsSeconds()
        {
            var baseAddress = "http://localhost:12345";
            var builder = new WebHostBuilder()
                .UseKestrel()
                .UseWebRoot(Directory.GetCurrentDirectory())
                .Configure(app => app.UseStaticFiles());

            using (var server = builder.Start(baseAddress))
            {
                using (var client = new HttpClient() { BaseAddress = new Uri(baseAddress) })
                {
                    var last = File.GetLastWriteTimeUtc("TestDocument.txt");
                    var response = await client.GetAsync("TestDocument.txt");
                    
                    var trimed = new DateTimeOffset(last.Year, last.Month, last.Day, last.Hour, last.Minute, last.Second, TimeSpan.Zero).ToUniversalTime();

                    Assert.Equal(response.Content.Headers.LastModified.Value, trimed);
                }
            }
        }

        [Theory]
        [MemberData(nameof(ExistingFiles))]
        public async Task FoundFile_Served_All(string baseUrl, string baseDir, string requestUrl)
        {
            await FoundFile_Served(baseUrl, baseDir, requestUrl);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData("", @".", "/testDocument.Txt")]
        [InlineData("/somedir", @".", "/somedir/Testdocument.TXT")]
        [InlineData("/SomeDir", @".", "/soMediR/testdocument.txT")]
        [InlineData("/somedir", @"SubFolder", "/somedir/Ranges.tXt")]
        public async Task FoundFile_Served_Windows(string baseUrl, string baseDir, string requestUrl)
        {
            await FoundFile_Served(baseUrl, baseDir, requestUrl);
        }

        public async Task FoundFile_Served(string baseUrl, string baseDir, string requestUrl)
        {
            var baseAddress = "http://localhost:12345";
            var builder = new WebHostBuilder()
                .UseKestrel()
                .UseWebRoot(Path.Combine(Directory.GetCurrentDirectory(), baseDir))
                .Configure(app => app.UseStaticFiles(new StaticFileOptions()
                {
                    RequestPath = new PathString(baseUrl),
                }));

            using (var server = builder.Start(baseAddress))
            {
                var hostingEnvironment = server.Services.GetService<IHostingEnvironment>();

                using (var client = new HttpClient() { BaseAddress = new Uri(baseAddress) })
                {
                    var fileInfo = hostingEnvironment.WebRootFileProvider.GetFileInfo(Path.GetFileName(requestUrl));
                    var response = await client.GetAsync(requestUrl);
                    var responseContent = await response.Content.ReadAsByteArrayAsync();

                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
                    Assert.True(response.Content.Headers.ContentLength == fileInfo.Length);
                    Assert.Equal(response.Content.Headers.ContentLength, responseContent.Length);

                    using (var stream = fileInfo.CreateReadStream())
                    {
                        var fileContents = new byte[stream.Length];
                        stream.Read(fileContents, 0, (int)stream.Length);
                        Assert.True(responseContent.SequenceEqual(fileContents));
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(ExistingFiles))]
        public async Task HeadFile_HeadersButNotBodyServed(string baseUrl, string baseDir, string requestUrl)
        {
            var baseAddress = "http://localhost:12345";
            var builder = new WebHostBuilder()
                .UseKestrel()
                .UseWebRoot(Path.Combine(Directory.GetCurrentDirectory(), baseDir))
                .Configure(app => app.UseStaticFiles(new StaticFileOptions()
                {
                    RequestPath = new PathString(baseUrl),
                }));

            using (var server = builder.Start(baseAddress))
            {
                var hostingEnvironment = server.Services.GetService<IHostingEnvironment>();

                using (var client = new HttpClient() { BaseAddress = new Uri(baseAddress) })
                {
                    var fileInfo = hostingEnvironment.WebRootFileProvider.GetFileInfo(Path.GetFileName(requestUrl));
                    var request = new HttpRequestMessage(HttpMethod.Head, requestUrl);
                    var response = await client.SendAsync(request);

                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
                    Assert.True(response.Content.Headers.ContentLength == fileInfo.Length);
                    Assert.Equal(0, (await response.Content.ReadAsByteArrayAsync()).Length);
                }
            }
        }

        public static IEnumerable<string[]> ExistingFiles => new[]
        {
            new[] {"", @".", "/TestDocument.txt"},
            new[] {"/somedir", @".", "/somedir/TestDocument.txt"},
            new[] {"/SomeDir", @".", "/soMediR/TestDocument.txt"},
            new[] {"", @"SubFolder", "/ranges.txt"},
            new[] {"/somedir", @"SubFolder", "/somedir/ranges.txt"},
            new[] {"", @"SubFolder", "/Empty.txt"}
        };
    }
}
