// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.IntegrationTesting;
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

        [Fact]
        public void ClientDisconnect_Kestrel_NoWriteExceptionThrown()
        {
            ClientDisconnect_NoWriteExceptionThrown(ServerType.Kestrel);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public void ClientDisconnect_WebListener_NoWriteExceptionThrown()
        {
            ClientDisconnect_NoWriteExceptionThrown(ServerType.WebListener);
        }

        public void ClientDisconnect_NoWriteExceptionThrown(ServerType serverType)
        {
            var baseAddress = "http://localhost:12345";
            var requestReceived = new ManualResetEvent(false);
            var requestCacelled = new ManualResetEvent(false);
            var responseComplete = new ManualResetEvent(false);
            Exception exception = null;
            var builder = new WebHostBuilder()
                .UseWebRoot(Path.Combine(Directory.GetCurrentDirectory()))
                .Configure(app =>
                {
                    app.Use(async (context, next) =>
                    {
                        try
                        {
                            requestReceived.Set();
                            Assert.True(requestCacelled.WaitOne(TimeSpan.FromSeconds(10)), "not cancelled");
                            Assert.True(context.RequestAborted.WaitHandle.WaitOne(TimeSpan.FromSeconds(10)), "not aborted");
                            await next();
                        }
                        catch (Exception ex)
                        {
                            exception = ex;
                        }
                        responseComplete.Set();
                    });
                    app.UseStaticFiles();
                });

            if (serverType == ServerType.WebListener)
            {
                builder.UseWebListener();
            }
            else if (serverType == ServerType.Kestrel)
            {
                builder.UseKestrel();
            }

            using (var server = builder.Start(baseAddress))
            {
                // We don't use HttpClient here because it's disconnect behavior varies across platforms.
                var socket = SendSocketRequestAsync(baseAddress, "/TestDocument1MB.txt");
                Assert.True(requestReceived.WaitOne(TimeSpan.FromSeconds(10)), "not received");

                socket.LingerState = new LingerOption(true, 0);
                socket.Dispose();
                requestCacelled.Set();

                Assert.True(responseComplete.WaitOne(TimeSpan.FromSeconds(10)), "not completed");
                Assert.Null(exception);
            }
        }

        private Socket SendSocketRequestAsync(string address, string path, string method = "GET")
        {
            var uri = new Uri(address);
            var builder = new StringBuilder();
            builder.AppendLine($"{method} {path} HTTP/1.1");
            builder.Append("HOST: ");
            builder.AppendLine(uri.Authority);
            builder.AppendLine();

            byte[] request = Encoding.ASCII.GetBytes(builder.ToString());

            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(uri.Host, uri.Port);
            socket.Send(request);
            return socket;
        }
    }
}
