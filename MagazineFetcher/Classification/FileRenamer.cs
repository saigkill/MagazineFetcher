// <copyright file="FileRenamer.cs" company="Sascha Manns">
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

using System.Text.RegularExpressions;

using Microsoft.Extensions.Configuration;

namespace MagazineFetcher.Classification;

public class FileRenamer
{
	private readonly string _pattern;

	public FileRenamer(IConfiguration configuration)
	{
		var settings = configuration.Get<AppConfig.AppConfig>();
		_pattern = settings.RenamePattern;
	}

	public string BuildNewName(string magazine, string originalName)
	{
		var year = ExtractYear(originalName);
		var issue = ExtractIssueNumber(originalName);

		return _pattern
			.Replace("{Magazine}", magazine)
			.Replace("{Issue}", issue)
			.Replace("{Year}", year);
	}

	public string ExtractIssueNumber(string name)
	{
		var digits = new string(name.Where(char.IsDigit).ToArray());
		return digits.Length > 0 ? digits : "Unbekannt";
	}

	public string ExtractYear(string name)
	{
		var match = Regex.Match(name, @"(20\d{2})");
		return match.Success ? match.Groups[1].Value : DateTime.Now.Year.ToString();
	}

	public string NormalizeFileName(string name)
	{
		var replacements = new Dictionary<string, string>
		{
			{ "Ç¬", "ü" },
			{ "ÇÏ", "ä" },
			{ "Çô", "ö" },
			{ "Ç¸", "é" },
			{ "ƒ??", "’" },
			{ "ƒ?", "’" },
			{ "ƒ", "’" },
			{ "Ç?", "e" }
		};

		foreach (var kv in replacements)
			name = name.Replace(kv.Key, kv.Value);

		return name;
	}
}
