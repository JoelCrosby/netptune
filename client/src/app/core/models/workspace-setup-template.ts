export interface WorkspaceSetupTemplate {
  key: string;
  name: string;
  description: string;
  isRecommended: boolean;
  statuses: SetupTemplateStatus[];
  tags: string[];
  relationTypes: SetupTemplateRelation[];
  boardGroups: string[];
}

export interface SetupTemplateStatus {
  name: string;
  color: string;
  category: number;
}

export interface SetupTemplateRelation {
  name: string;
  inverseName: string;
  color: string;
  category: number;
}
