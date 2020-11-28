export interface AddTagRequest {
  tag: string;
  workspaceSlug: string;
}

export interface AddTagToTaskRequest extends AddTagRequest {
  systemId: string;
}
