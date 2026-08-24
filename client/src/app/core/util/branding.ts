import { environment } from '@env/environment';

const mebibyte = 1024 * 1024;

export const brandingImageMaxBytes = 10 * mebibyte;

export const brandingImageAcceptTypes = [
  'image/png',
  'image/jpeg',
  'image/webp',
  'image/gif',
  'image/avif',
];

export const brandingImageAccept = brandingImageAcceptTypes.join(',');

// The interceptor only prefixes requests it routes; an <img> src has to be absolute itself.
export function brandingImageUrl(
  workspaceSlug: string | undefined,
  fileId: string | null | undefined
): string | null {
  if (!workspaceSlug || !fileId) return null;

  const base = environment.apiEndpoint.replace(/\/+$/, '');

  return `${base}/api/workspaces/${workspaceSlug}/files/${fileId}/content?disposition=inline`;
}

export function isBrandingImageType(file: File): boolean {
  return brandingImageAcceptTypes.includes(file.type.toLowerCase());
}
