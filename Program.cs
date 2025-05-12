using AsyncAwaitBestPractices;
using Microsoft.Extensions.Configuration;
using OllamaSharp;
using OllamaSharp.Models;

namespace owk;

internal class Program
{
	static async Task Main(string[] args)
	{
		var config = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
			.Build();

		var settings = new Settings();
		config.Bind(settings);

		var cancellationSource = new CancellationTokenSource();

		var client = new OllamaApiClient(settings.Uri);

		while (!cancellationSource.IsCancellationRequested)
		{
			var watch = System.Diagnostics.Stopwatch.StartNew();

			var runningModels = await client.ListRunningModelsAsync(cancellationSource.Token);
			var runningModelCount = runningModels.Count();
			var usedVramGb = runningModels.Sum(m => m.SizeVram) / 1000000000;
			Log($"{runningModelCount} {(runningModelCount == 1 ? "model" : "models")} running with {usedVramGb} GB in total.");

			if (runningModelCount == 0)
				Log("No models running.");
			else
				Log($"{usedVramGb} GB VRAM in use by {runningModelCount} running {(runningModelCount == 1 ? "model" : "models")}: {string.Join(", ", runningModels.Select(m => m.Name))}");

			foreach (var model in settings.Models)
			{
				var isRunning = runningModels.Any(m => m.Name.Equals(model.Name, StringComparison.OrdinalIgnoreCase));
				var shouldUnload = model.KeepAlive == "0";

				//if (isRunning)
				//{
				//	if (shouldUnload)
				//	{
				//		Log($"Unloading {model.Name} ...");
				//		var request = new GenerateRequest { Prompt = "Respond with a single character /no_think", Model = model.Name, KeepAlive = model.KeepAlive, Stream = false };
				//		client.GenerateAsync(request, cancellationSource.Token).StreamToEndAsync().SafeFireAndForget();
				//	}
				//	else
				//	{
				//		Log($"{model.Name} is already running.");
				//	}
				//}

				//if (isRunning || shouldUnload)
				//	continue;

				if (ConditionEvaluator.CheckNumericCondition(model.Condition.VramGbInUse, usedVramGb))
				{
					if (isRunning)
					{
						Log($"Waking up '{model.Name}' ...");
					}
					else
					{
						if (shouldUnload)
							Log($"Unloading '{model.Name}' ...");
						else
							Log($"Updating keep alive '{model.Name}' ...");
					}

					var request = new GenerateRequest { Prompt = "Respond with a single character /no_think", Model = model.Name, KeepAlive = model.KeepAlive, Stream = false };
					client.GenerateAsync(request, cancellationSource.Token).StreamToEndAsync().SafeFireAndForget();
				}
				else
				{
					Log($"{model.Name} is not running but conditions are not met.");
				}
			}

			watch.Stop();

			Log($"Sleeping for {settings.CheckInterval - watch.Elapsed}.");
			await Task.Delay(settings.CheckInterval - watch.Elapsed);
			Log("-----");
		}
	}

	private static void Log(string message) => Console.WriteLine($"{DateTime.Now}: {message}");
}
