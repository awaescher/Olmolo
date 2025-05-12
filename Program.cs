using AsyncAwaitBestPractices;
using Microsoft.Extensions.Configuration;
using OllamaSharp;
using OllamaSharp.Models;

namespace olmolo;

internal class Program
{
	static async Task Main(string[] args)
	{
		var config = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
			.Build();

		var settings = new Settings();
		config.Bind(settings);

		if (settings.CheckInterval < TimeSpan.FromMinutes(1))
		{
			Log("Warning: The check interval might be too short so that Ollama might not");
			Log("         finish loading or unloading models until the next check.");
			Log("         This might lead to unwanted behavior like unwillingly");
			Log("         reactivating models that Ollama is unloading at that very moment.");
			Log("         Keep an eye on the logs and consider increasing the check interval.");
		}

		var cancellationSource = new CancellationTokenSource();

		var client = new OllamaApiClient(settings.Uri);

		var vram = ConditionEvaluator.Parse(settings.TotalVram);
		var totalVramGb = vram.Value;

		while (!cancellationSource.IsCancellationRequested)
		{
			var watch = System.Diagnostics.Stopwatch.StartNew();

			var activeModels = await client.ListRunningModelsAsync(cancellationSource.Token);
			var activeModelCount = activeModels.Count();
			var usedVramGb = activeModels.Sum(m => m.SizeVram) / 1000000000;
			var freeVramGb = totalVramGb - usedVramGb;

			Log($"Active models:  {(activeModels.Any() ? string.Join(", ", activeModels.Select(m => m.Name)) : "-")}");
			Log($"VRAM total:     {totalVramGb} GB");
			Log($"VRAM used:      {usedVramGb} GB");
			Log($"VRAM free:      {freeVramGb} GB");

			foreach (var model in settings.Models)
			{
				var run = activeModels.FirstOrDefault(m => m.Name.Equals(model.Name, StringComparison.OrdinalIgnoreCase));

				var currentRule = 0;
				foreach (var rule in model.Rules)
				{
					currentRule++;

					// skip unloading if the model is not active
					if (rule.IsUnloadingRule && run is null)
						continue;

					// skip calling if the model is active and does not expire soon
					if (!rule.IsUnloadingRule && run?.ExpiresAt is DateTime exp && exp > DateTime.UtcNow + settings.CheckInterval)
					{
						Log($"No need to extend keep_alive for {model.Name} now");
						Log($" └ because the remaining keep_alive (until {exp.ToLocalTime().ToShortTimeString()}) is longer than the check interval");
						continue;
					}

					if (ConditionEvaluator.CheckNumericCondition(rule.VramFree, freeVramGb))
					{
						var verb = rule.IsUnloadingRule ? "Unloading" : (run is null ? "Loading" : "Extending");
						Log($"{verb} {model.Name} with keep_alive={rule.KeepAlive}");
						if (rule.IsUnloadingRule || run is null)
							Log($" └ because free vram is {rule.VramFree}.");
						else
							Log($" └ because free vram is {rule.VramFree} and the model is expiring at {run!.ExpiresAt.ToLocalTime().ToShortTimeString()} which is before the next iteration.");

						var request = new GenerateRequest { Prompt = "Respond with a single character /no_think", Model = model.Name, KeepAlive = rule.KeepAlive, Stream = false };
						client.GenerateAsync(request, cancellationSource.Token).StreamToEndAsync().SafeFireAndForget(Log);
					}
				}
			}

			watch.Stop();

			Log($"Done. Sleeping for {settings.CheckInterval - watch.Elapsed}.");
			Log("");

			await Task.Delay(settings.CheckInterval - watch.Elapsed);
		}
	}

	private static void Log(string message) => Console.WriteLine(string.IsNullOrEmpty(message) ? "" : $"{DateTime.Now}: {message}");

	private static void Log(Exception exception) => Log("ERROR: " + exception.Message);
}
