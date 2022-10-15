namespace Netptune.Core.Ordering;

public static class OrderingUtils
{
    public static double GetNewSortOrder(double? preOrder, double? nextOrder)
    {
        if (!nextOrder.HasValue && preOrder.HasValue)
        {
            return preOrder.Value + 1;
        }

        if (nextOrder.HasValue && !preOrder.HasValue)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return nextOrder.Value == 0 ? -1 : nextOrder.Value * 0.9;
        }

        if (nextOrder.HasValue && preOrder.HasValue)
        {
            return (preOrder.Value + nextOrder.Value) / 2;
        }

        return 1;
    }
}
