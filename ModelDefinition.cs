using System.Diagnostics;

namespace olmolo;

/// <summary>
/// Defines a specific AI model configuration and its activation conditions.
/// </summary>
[DebuggerDisplay("{Name}")]
public class ModelDefinition
{
	/// <summary>
	/// The name of the model as recognized by Ollama.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// The conditions under which this model should be activated.
	/// </summary>
	public IEnumerable<Rule> Rules { get; set; } = [];
}

/// <summary>
/// Specifies the conditions under which a model should be activated.
/// </summary>
[DebuggerDisplay("If vram free {VramFree}, then set keep_alive to {KeepAlive}")]
public class Rule
{
	/// <summary>
	/// The amount of VRAM used by Ollama in total.
	/// Use operators and values like "<50GB", "<=50GB", ">=50GB", ">50GB", "=50GB" or "!=50GB"
	/// </summary>
	public string VramFree { get; set; } = string.Empty;

	/// <summary>
	/// How long to keep the model loaded in memory after last use.
	/// Format is a duration string (e.g., "10m" for 10 minutes).
	/// </summary>
	public string KeepAlive { get; set; } = "10m";

	public bool IsUnloadingRule => KeepAlive == "0" || KeepAlive == "0s" || KeepAlive == "0m" || KeepAlive == "0h";
}
