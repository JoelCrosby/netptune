export enum RelationCategory {
  hierarchy = 0,
  dependency = 1,
  related = 2,
  duplicate = 3,
}

export const relationCategoryLabels: Record<RelationCategory, string> = {
  [RelationCategory.hierarchy]: 'Hierarchy',
  [RelationCategory.dependency]: 'Dependency',
  [RelationCategory.related]: 'Related',
  [RelationCategory.duplicate]: 'Duplicate',
};

export const relationCategoryDescriptions: Record<RelationCategory, string> = {
  [RelationCategory.hierarchy]:
    'Parent and child. A task can have only one parent, and a task cannot be its own ancestor.',
  [RelationCategory.dependency]:
    'One task waits on another. Dependencies cannot form a loop.',
  [RelationCategory.related]:
    'Reads the same in both directions, so it has no inverse name.',
  [RelationCategory.duplicate]: 'One task supersedes another.',
};

export const relationCategoryOptions = [
  RelationCategory.hierarchy,
  RelationCategory.dependency,
  RelationCategory.related,
  RelationCategory.duplicate,
];

export const isSymmetricCategory = (category: RelationCategory) => {
  return category === RelationCategory.related;
};

export interface RelationType {
  id: number;
  workspaceId: number;
  name: string;
  inverseName: string;
  key: string;
  description?: string | null;
  color?: string | null;
  sortOrder: number;
  category: RelationCategory;
  isSystem: boolean;
}

export interface CreateRelationTypeRequest {
  name: string;
  inverseName?: string | null;
  description?: string | null;
  color?: string | null;
  category: RelationCategory;
}

export interface UpdateRelationTypeRequest {
  id: number;
  name: string;
  inverseName?: string | null;
  description?: string | null;
  color?: string | null;
}

export interface ReorderRelationTypesRequest {
  relationTypeIds: number[];
}
