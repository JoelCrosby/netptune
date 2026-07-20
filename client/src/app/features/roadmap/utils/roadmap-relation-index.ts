export interface RoadmapRelationIndexItem<TCategory> {
  sourceTaskId: number;
  targetTaskId: number;
  category: TCategory;
}

export const countOffscreenDependencies = <TCategory>(
  relations: readonly RoadmapRelationIndexItem<TCategory>[],
  visibleTaskIds: ReadonlySet<number>,
  dependencyCategory: TCategory
): ReadonlyMap<number, number> => {
  const counts = new Map<number, number>();

  for (const relation of relations) {
    if (
      relation.category === dependencyCategory &&
      visibleTaskIds.has(relation.targetTaskId) &&
      !visibleTaskIds.has(relation.sourceTaskId)
    ) {
      counts.set(
        relation.targetTaskId,
        (counts.get(relation.targetTaskId) ?? 0) + 1
      );
    }
  }

  return counts;
};
