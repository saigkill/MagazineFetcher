// <copyright file="FileRenamerTest.cs" company="Sascha Manns">
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

using JetBrains.Annotations;

using FluentAssertions;

using MagazineFetcher.AppConfig;
using MagazineFetcher.Classification;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using Xunit;

namespace MagazineFetcher.Tests.Classification;

[TestSubject(typeof(FileRenamer))]
public class FileRenamerTest
{
	private readonly FileRenamer _fileRenamer;

	public FileRenamerTest()
	{
		var inMemorySettings = new Configuration()
		{
			RenamePattern = "{Magazine}-{Issue}-{Year}.pdf"
		};

		var options = Options.Create(inMemorySettings);

		_fileRenamer = new FileRenamer(options);
	}

	[Theory]
	[InlineData("TechMag", "TechMag2023Issue5.pdf", "TechMag-5-2023.pdf")]
	[InlineData("ScienceToday", "ScienceToday2022Issue10.pdf", "ScienceToday-10-2022.pdf")]
	[InlineData("Nature", "NatureIssueUnknown.pdf", "Nature-Unbekannt-2026.pdf")]
	[InlineData("History", "History2021.pdf", "History-Unbekannt-2021.pdf")]
	public void BuildNewName_ShouldReturnCorrectlyFormattedName(string magazine, string originalName, string expected)
	{
		// Act
		var result = _fileRenamer.BuildNewName(magazine, originalName);

		// Assert
		result.Should().Be(expected);
	}

	[Theory]
	[InlineData("TechMag2023Issue5.pdf", "5")]
	[InlineData("ScienceToday2022Issue10.pdf", "10")]
	[InlineData("NatureIssueUnknown.pdf", "Unbekannt")]
	[InlineData("History2021.pdf", "Unbekannt")]
	public void ExtractIssueNumber_ShouldExtractCorrectIssueNumber(string name, string expected)
	{
		// Act
		var result = _fileRenamer.ExtractIssueNumber(name);

		// Assert
		result.Should().Be(expected);
	}

	[Theory]
	[InlineData("TechMag2023Issue5.pdf", "2023")]
	[InlineData("ScienceToday2022Issue10.pdf", "2022")]
	[InlineData("NatureIssueUnknown.pdf", "2026")]
	[InlineData("History2021.pdf", "2021")]
	public void ExtractYear_ShouldExtractCorrectYear(string name, string expected)
	{
		// Act
		var result = _fileRenamer.ExtractYear(name);

		// Assert
		result.Should().Be(expected);
	}

	[Theory]
	[InlineData("Ç¬ÇÏÇôÇ¸ƒ??", "üäöé’")]
	[InlineData("Ç¬ÇÏÇôÇ¸ƒ?", "üäöé’")]
	[InlineData("Ç¬ÇÏÇôÇ¸ƒ", "üäöé’")]
	[InlineData("Ç?", "e")]
	[InlineData("NormalName", "NormalName")]
	public void NormalizeFileName_ShouldReplaceSpecialCharacters(string input, string expected)
	{
		// Act
		var result = _fileRenamer.NormalizeFileName(input);

		// Assert
		result.Should().Be(expected);
	}
}
