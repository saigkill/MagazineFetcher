// <copyright file="ConsoleHostedService.cs" company="Sascha Manns">
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

using MagazineFetcher.Rss;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MagazineFetcher.Hosting;

internal sealed class ConsoleHostedService : IHostedService
{
	private readonly ILogger<ConsoleHostedService> _logger;
	private readonly IHostApplicationLifetime _appLifetime;
	private readonly MagazineFetcher _magazineFetcherService;

	public ConsoleHostedService(
		ILogger<ConsoleHostedService> logger,
		IHostApplicationLifetime appLifetime,
		MagazineFetcher magazineFetcherService)
	{
		_logger = logger;
		_appLifetime = appLifetime;
		_magazineFetcherService = magazineFetcherService;
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		try
		{
			_logger.LogInformation("Running the App.");
			await _magazineFetcherService.StartAsync();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Unhandled exception!");
		}
		finally
		{
			_appLifetime.StopApplication();
		}
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}
}
