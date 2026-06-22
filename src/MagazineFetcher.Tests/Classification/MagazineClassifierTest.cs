// <copyright file="MagazineClassifierTest.cs" company="Sascha Manns">
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

using MagazineFetcher.Classification;

using FluentAssertions;
using MagazineFetcher.AppConfig;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

using Xunit;

namespace MagazineFetcher.Tests.Classification;

[TestSubject(typeof(MagazineClassifier))]
public class MagazineClassifierTest
{
	private readonly Mock<ILogger<MagazineClassifier>> _mockLogger;
	private readonly MagazineClassifier _classifier;

	public MagazineClassifierTest()
	{
		var optionsConfig = new MagazineFetcherOptions()
		{
			MagazineMapping =
			{
				["Der Spiegel"] = "/share/Multimedia/Magazines/Der Spiegel",
				["Focus"] = "/share/Multimedia/Magazines/Focus",
				["Wirtschaftswoche"] = "/share/Multimedia/Magazines/Wirtschaftswoche"
			}
		};

		var options = Options.Create(optionsConfig);

		_mockLogger = new Mock<ILogger<MagazineClassifier>>();
		_classifier = new MagazineClassifier(options, _mockLogger.Object);
	}

	[Theory]
	[InlineData("Der Spiegel", "/share/Multimedia/Magazines/Der Spiegel")]
	[InlineData("Focus", "/share/Multimedia/Magazines/Focus")]
	[InlineData("Wirtschaftswoche", "/share/Multimedia/Magazines/Wirtschaftswoche")]
	[InlineData("UnknownFile.pdf", null)]
	[InlineData("", null)]
	public void Classify_ShouldReturnExpectedClassification(string fileName, string expectedClassification)
	{
		// Act
		var result = _classifier.Classify(fileName);

		// Assert
		result.Should().Be(expectedClassification);
	}

	[Fact]
	public void Classify_ShouldReturnNull_WhenFileNameIsNull()
	{
		// Arrange & Act
		Func<string> act = () => _classifier.Classify(null);

		// Assert
		act.Should().Throw<NullReferenceException>();
	}
}
