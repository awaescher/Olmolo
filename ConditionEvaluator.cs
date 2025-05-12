namespace okai;

public static class ConditionEvaluator
{
	public record NumericCriteria(string Operator, int Value, string Unit);

	public static NumericCriteria Parse(string value)
	{
		value = value.Replace(" ", "").Trim();

		var numericValue = 0;
		var unit = "GB";
		if (!value.EndsWith("GB", StringComparison.OrdinalIgnoreCase))
			throw new NotSupportedException("Only GB is supported as unit. Given was " + value);

		value = value.Replace(unit, "").Trim();

		if (int.TryParse(value, out numericValue))
			return new NumericCriteria(Operator: "=", numericValue, unit);

		foreach (var op in new string[] { ">=", "<=", ">", "<", "=", "==", "!=", "<>" })
		{
			if (value.StartsWith(op))
			{
				numericValue = int.Parse(value.Substring(op.Length));
				return new NumericCriteria(Operator: op, numericValue, unit);
			}
		}

		throw new NotSupportedException("Could not parse value: " + value);
	}

	public static bool CheckNumericCondition(string condition, long referenceValue)
	{
		var criteria = Parse(condition);

		return criteria.Operator switch
		{
			">=" => referenceValue >= criteria.Value,
			"<=" => referenceValue <= criteria.Value,
			">" => referenceValue > criteria.Value,
			"<" => referenceValue < criteria.Value,
			"==" => referenceValue == criteria.Value,
			"=" => referenceValue == criteria.Value,
			"!=" => referenceValue != criteria.Value,
			"<>" => referenceValue != criteria.Value,
			_ => throw new NotSupportedException($"Operator '{criteria.Operator}' is not supported."),
		};
	}
}
