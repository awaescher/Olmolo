using System.Diagnostics;

namespace olmolo;

/// <summary>
/// Configuration settings for the Ollama model management system.
/// </summary>
[DebuggerDisplay("Check {Uri} every {CheckInterval}")]
public class Settings
{
	/// <summary>
	/// Specifies how frequently the system should check the models.
	/// </summary>
	public TimeSpan CheckInterval { get; set; }

	/// <summary>
	/// The endpoint URI where Ollama is accessible.
	/// </summary>
	public Uri OllamaUri { get; set; } = new Uri("http://localhost:11434");

	/// <summary>
	/// The collection of models to be managed by the system.
	/// </summary>
	public IEnumerable<ModelDefinition> Models { get; set; } = [];

	public string TotalVram { get; set; } = string.Empty;
}
