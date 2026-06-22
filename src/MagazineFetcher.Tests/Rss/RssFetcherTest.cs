// <copyright file="RssFetcherTest.cs" company="Sascha Manns">
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

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using FluentAssertions;

using JetBrains.Annotations;

using MagazineFetcher.AppConfig;
using MagazineFetcher.Rss;

using Microsoft.Extensions.Logging;

using Moq;
using Moq.Protected;

using Xunit;

namespace MagazineFetcher.Tests.Rss;

[TestSubject(typeof(RssFetcher))]
public class RssFetcherTest
{
	[Fact]
	public async Task GetMatchingItemAsync_ShouldReturnMatchingItem_WhenTitleContainsMatch()
	{
		// Arrange
		var mockLogger = new Mock<ILogger<RssFetcher>>();
		var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
		var rssContent =
			@"<rss><channel><item><title>Matching Title</title><link>http://example.com</link><pubDate>2026-01-01</pubDate></item></channel></rss>";

		mockHttpMessageHandler.Protected()
			.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.OK,
				Content = new StringContent(rssContent)
			});

		var httpClient = new HttpClient(mockHttpMessageHandler.Object);
		var fetcher = new RssFetcher(mockLogger.Object)
		{
			_client = httpClient
		};

		var filter = new RssFilterConfig
		{
			TitleContains = new List<string> { "Matching" }
		};

		// Act
		var result = await fetcher.GetMatchingItemAsync("http://example.com/rss", filter);

		// Assert
		result.Should().NotBeNull();
		result!.Title.Should().Be("Matching Title");
		result.Link.Should().Be("http://example.com");
		result.PubDate.Should().Be("2026-01-01");
	}

	[Fact]
	public async Task GetMatchingItemAsync_ShouldReturnNull_WhenNoTitleContainsMatch()
	{
		// Arrange
		var mockLogger = new Mock<ILogger<RssFetcher>>();
		var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
		var rssContent =
			@"<rss><channel><item><title>Non-Matching Title</title><link>http://example.com</link><pubDate>2026-01-01</pubDate></item></channel></rss>";

		mockHttpMessageHandler.Protected()
			.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.OK,
				Content = new StringContent(rssContent)
			});

		var httpClient = new HttpClient(mockHttpMessageHandler.Object);
		var fetcher = new RssFetcher(mockLogger.Object)
		{
			_client = httpClient
		};

		var filter = new RssFilterConfig
		{
			TitleContains = new List<string> { "Non-Existent" }
		};

		// Act
		var result = await fetcher.GetMatchingItemAsync("http://example.com/rss", filter);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task GetMatchingItemAsync_ShouldReturnMatchingItem_WhenTitleMatchesRegex()
	{
		// Arrange
		var mockLogger = new Mock<ILogger<RssFetcher>>();
		var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
		var rssContent =
			@"<rss><channel><item><title>Regex Match</title><link>http://example.com</link><pubDate>2026-01-01</pubDate></item></channel></rss>";

		mockHttpMessageHandler.Protected()
			.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.OK,
				Content = new StringContent(rssContent)
			});

		var httpClient = new HttpClient(mockHttpMessageHandler.Object);
		var fetcher = new RssFetcher(mockLogger.Object)
		{
			_client = httpClient
		};

		var filter = new RssFilterConfig
		{
			TitleRegex = "Regex.*"
		};

		// Act
		var result = await fetcher.GetMatchingItemAsync("http://example.com/rss", filter);

		// Assert
		result.Should().NotBeNull();
		result!.Title.Should().Be("Regex Match");
		result.Link.Should().Be("http://example.com");
		result.PubDate.Should().Be("2026-01-01");
	}

	[Fact]
	public async Task GetMatchingItemAsync_ShouldReturnNull_WhenNoTitleMatchesRegex()
	{
		// Arrange
		var mockLogger = new Mock<ILogger<RssFetcher>>();
		var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
		var rssContent =
			@"<rss><channel><item><title>No Match</title><link>http://example.com</link><pubDate>2026-01-01</pubDate></item></channel></rss>";

		mockHttpMessageHandler.Protected()
			.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
				ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.OK,
				Content = new StringContent(rssContent)
			});

		var httpClient = new HttpClient(mockHttpMessageHandler.Object);
		var fetcher = new RssFetcher(mockLogger.Object)
		{
			_client = httpClient
		};

		var filter = new RssFilterConfig
		{
			TitleRegex = "NonExistent.*"
		};

		// Act
		var result = await fetcher.GetMatchingItemAsync("http://example.com/rss", filter);

		// Assert
		result.Should().BeNull();
	}
}
