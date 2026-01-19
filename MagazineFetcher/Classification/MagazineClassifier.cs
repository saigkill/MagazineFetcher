// <copyright file="MagazineClassifier.cs" company="Sascha Manns">
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

using MagazineFetcher.AppConfig;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MagazineFetcher.Classification;

public class MagazineClassifier(IOptions<Configuration> configuration, ILogger<MagazineClassifier> logger)
{
	private readonly Dictionary<string, string> _mapping = configuration.Value.MagazineMapping ?? throw new InvalidOperationException("MagazineMapping configuration is missing");

	internal string? Classify(string fileName)
	{
		foreach (var kv in _mapping)
		{
			try
			{
				if (fileName.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
					return kv.Value;
			}
			catch (Exception ex)
			{
				logger.LogError($"Problem while classifying: {ex.Message}", ex);
				throw;
			}
		}

		return null;
	}
}
