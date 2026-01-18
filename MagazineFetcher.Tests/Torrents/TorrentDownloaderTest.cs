// <copyright file="TorrentDownloaderTest.cs" company="Sascha Manns">
// Copyright (c) 2026 Sascha Manns.
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the “Software”), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
// ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH
// THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using JetBrains.Annotations;

using MagazineFetcher.Torrents;

using Microsoft.Extensions.Logging;

using Moq;
using Moq.Protected;

using Xunit;

namespace MagazineFetcher.Tests.Torrents;

[TestSubject(typeof(TorrentDownloader))]
public class TorrentDownloaderTest
{
	private readonly Mock<ILogger<TorrentDownloader>> _loggerMock;
	private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
	private readonly TorrentDownloader _torrentDownloader;

	public TorrentDownloaderTest()
	{
		_loggerMock = new Mock<ILogger<TorrentDownloader>>();
		_httpMessageHandlerMock = new Mock<HttpMessageHandler>();
		var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
		_torrentDownloader = new TorrentDownloader(_loggerMock.Object)
		{
			_client = httpClient
		};
	}

	[Theory]
	[InlineData("https://example.com/torrent1", new byte[] { 1, 2, 3 })]
	[InlineData("https://example.com/torrent2", new byte[] { 4, 5, 6 })]
	public async Task DownloadTorrentAsync_ShouldReturnByteArray_WhenUrlIsValid(string url, byte[] expectedBytes)
	{
		// Arrange
		_httpMessageHandlerMock
			.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.Is<HttpRequestMessage>(req => req.RequestUri != null && req.RequestUri.ToString() == url),
				ItExpr.IsAny<CancellationToken>()
			)
			.ReturnsAsync(new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.OK,
				Content = new ByteArrayContent(expectedBytes)
			});

		// Act
		var result = await _torrentDownloader.DownloadTorrentAsync(url);

		// Assert
		result.Should().BeEquivalentTo(expectedBytes);
	}

	[Theory]
	[InlineData("https://example.com/invalid")]
	[InlineData("https://example.com/notfound")]
	public async Task DownloadTorrentAsync_ShouldThrowException_WhenUrlIsInvalid(string url)
	{
		// Arrange
		_httpMessageHandlerMock
			.Protected()
			.Setup<Task<HttpResponseMessage>>(
				"SendAsync",
				ItExpr.Is<HttpRequestMessage>(req => req.RequestUri != null && req.RequestUri.ToString() == url),
				ItExpr.IsAny<CancellationToken>()
			)
			.ThrowsAsync(new HttpRequestException("Error"));

		// Act
		Func<Task> act = async () => await _torrentDownloader.DownloadTorrentAsync(url);

		// Assert
		await act.Should().ThrowAsync<HttpRequestException>();
	}

	[Fact]
	public async Task DownloadTorrentAsync_ShouldThrowException_WhenUrlIsNull()
	{
		// Act
		Func<Task> act = async () => await _torrentDownloader.DownloadTorrentAsync(null!);

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>();
	}

	[Fact]
	public async Task DownloadTorrentAsync_ShouldThrowException_WhenUrlIsEmpty()
	{
		// Act
		Func<Task> act = async () => await _torrentDownloader.DownloadTorrentAsync(string.Empty);

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>();
	}
}
