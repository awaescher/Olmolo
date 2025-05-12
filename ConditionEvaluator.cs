namespace owk;

public static class ConditionEvaluator
{
	public static bool CheckNumericCondition(string condition, long referenceValue)
	{
		condition = condition.Replace(" ", "").Trim();

		if (condition.StartsWith("<="))
		{
			var conditionValue = long.Parse(condition[2..]);
			return referenceValue <= conditionValue;
		}
		else if (condition.StartsWith(">="))
		{
			var conditionValue = long.Parse(condition[2..]);
			return referenceValue < conditionValue;
		}
		else if (condition.StartsWith("<"))
		{
			var conditionValue = long.Parse(condition[1..]);
			return referenceValue < conditionValue;
		}
		else if (condition.StartsWith(">"))
		{
			var conditionValue = long.Parse(condition[1..]);
			return referenceValue > conditionValue;
		}
		else if (condition.StartsWith("="))
		{
			var conditionValue = long.Parse(condition[1..]);
			return referenceValue == conditionValue;
		}
		else if (condition.StartsWith("!="))
		{
			var conditionValue = long.Parse(condition[2..]);
			return referenceValue != conditionValue;
		}

		return false;
	}
}
