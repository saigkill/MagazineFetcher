// <copyright file="TorrentHistoryTest.cs" company="Sascha Manns">
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

using System.IO;

using FluentAssertions;

using JetBrains.Annotations;

using Microsoft.Extensions.Configuration;

using Moq;

using Xunit;

using MagazineFetcher.Torrents;

namespace MagazineFetcher.Tests.Torrents;

[TestSubject(typeof(TorrentHistory))]
public class TorrentHistoryTest
{
	private readonly IConfiguration _configuration;
	private readonly string _tempHistoryFile;

	public TorrentHistoryTest()
	{
		_tempHistoryFile = Path.GetTempFileName();

		var inMemorySettings = new Dictionary<string, string>
		{
			{ "HistoryFile", _tempHistoryFile }
		};

		_configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(inMemorySettings)
			.Build();
	}

	[Fact]
	public void Constructor_ShouldCreateHistoryFile_WhenFileDoesNotExist()
	{
		// Arrange
		File.Delete(_tempHistoryFile);

		// Act
		var torrentHistory = new TorrentHistory(_configuration);

		// Assert
		File.Exists(_tempHistoryFile).Should().BeTrue();
	}

	[Fact]
	public void Constructor_ShouldNotOverwriteExistingHistoryFile()
	{
		// Arrange
		File.WriteAllText(_tempHistoryFile, "ExistingContent");

		// Act
		var torrentHistory = new TorrentHistory(_configuration);

		// Assert
		File.ReadAllText(_tempHistoryFile).Should().Be("ExistingContent");
	}

	[Theory]
	[InlineData("Title1", true)]
	[InlineData("Title2", false)]
	public void AlreadyProcessed_ShouldReturnCorrectResult(string title, bool expectedResult)
	{
		// Arrange
		File.WriteAllLines(_tempHistoryFile, new[] { "Title1" });
		var torrentHistory = new TorrentHistory(_configuration);

		// Act
		var result = torrentHistory.AlreadyProcessed(title);

		// Assert
		result.Should().Be(expectedResult);
	}

	[Fact]
	public void MarkProcessed_ShouldAppendTitleToHistoryFile()
	{
		// Arrange
		var torrentHistory = new TorrentHistory(_configuration);
		var title = "NewTitle";

		// Act
		torrentHistory.MarkProcessed(title);

		// Assert
		File.ReadAllLines(_tempHistoryFile).Should().Contain(title);
	}

	[Fact]
	public void MarkProcessed_ShouldHandleEmptyHistoryFile()
	{
		// Arrange
		File.WriteAllText(_tempHistoryFile, string.Empty);
		var torrentHistory = new TorrentHistory(_configuration);
		var title = "FirstTitle";

		// Act
		torrentHistory.MarkProcessed(title);

		// Assert
		File.ReadAllLines(_tempHistoryFile).Should().ContainSingle().Which.Should().Be(title);
	}

	[Fact]
	public void AlreadyProcessed_ShouldReturnFalse_WhenHistoryFileIsEmpty()
	{
		// Arrange
		File.WriteAllText(_tempHistoryFile, string.Empty);
		var torrentHistory = new TorrentHistory(_configuration);
		var title = "NonExistentTitle";

		// Act
		var result = torrentHistory.AlreadyProcessed(title);

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public void AlreadyProcessed_ShouldBeCaseSensitive()
	{
		// Arrange
		File.WriteAllLines(_tempHistoryFile, new[] { "Title" });
		var torrentHistory = new TorrentHistory(_configuration);

		// Act
		var result = torrentHistory.AlreadyProcessed("title");

		// Assert
		result.Should().BeFalse();
	}
}
