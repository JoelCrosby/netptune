using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Ordering;

public static class SortOrdering
{
    // Sort orders drift as rows are created, deleted and imported, so the whole run is renumbered
    // rather than swapping two values: a page of the list cannot see the gaps or ties it would leave.
    public static List<T>? Move<T>(List<T> ordered, int id, SortMoveDirection direction)
        where T : IKeyedEntity<int>, ISortOrderedEntity
    {
        var currentIndex = ordered.FindIndex(item => item.Id == id);

        if (currentIndex < 0)
        {
            return null;
        }

        var targetIndex = direction == SortMoveDirection.Up ? currentIndex - 1 : currentIndex + 1;
        var isOutOfRange = targetIndex < 0 || targetIndex >= ordered.Count;

        if (isOutOfRange)
        {
            return [];
        }

        (ordered[currentIndex], ordered[targetIndex]) = (ordered[targetIndex], ordered[currentIndex]);

        var renumbered = new List<T>();

        for (var index = 0; index < ordered.Count; index++)
        {
            var item = ordered[index];
            var alreadyInPosition = Math.Abs(item.SortOrder - index) < double.Epsilon;

            if (alreadyInPosition)
            {
                continue;
            }

            item.SortOrder = index;
            renumbered.Add(item);
        }

        return renumbered;
    }
}
