export interface AddCommentRequest {
  comment: string;
  systemId: string;
  mentions: string[];
}

export interface UpdateCommentRequest {
  comment: string;
  mentions: string[];
}
