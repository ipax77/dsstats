namespace pax.dsstats.shared.Services;

public static class Normalize
{
    public static List<double> NormalizeList(List<double> inputList)
    {
        List<double> normalizedList = new List<double>();

        double minValue = inputList.Min();
        double maxValue = inputList.Max();

        double range = maxValue - minValue;

        if (range == 0)
        {
            for (int i = 0; i < inputList.Count; i++)
            {
                normalizedList.Add(0.5);
            }
            return normalizedList;
        }
        
        for (int i = 0; i < inputList.Count; i++)
        {
            normalizedList.Add((inputList[i] - minValue) / range);
        }

        return normalizedList;
    }
}
