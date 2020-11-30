export interface AddTagRequest {
  tag: string;
}

export interface AddTagToTaskRequest extends AddTagRequest {
  systemId: string;
}
