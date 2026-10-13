// Streamed assistant requests go through fetch, which the HTTP interceptors never see, so they set
// the workspace header themselves.
export function aiRequestHeaders(
  workspaceRoute: string | null,
  workspaceSlug: string | undefined
): Record<string, string> {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
  };

  const workspace = workspaceRoute ?? workspaceSlug;

  if (workspace) {
    headers['workspace'] = workspace;
  }

  return headers;
}
