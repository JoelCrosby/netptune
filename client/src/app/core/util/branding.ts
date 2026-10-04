import { environment } from '@env/environment';
import { formatBytes } from '@core/util/bytes';

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

export function brandingImageError(file: File) {
  const isSupportedType = isBrandingImageType(file);

  if (!isSupportedType) {
    return $localize`:Validation error when a chosen file is not a supported image:Choose a PNG, JPEG, WebP, GIF or AVIF image.`;
  }

  if (file.size > brandingImageMaxBytes) {
    const maxBytesLabel = formatBytes(brandingImageMaxBytes);

    return $localize`:Validation error when a chosen image is too large. SIZE is a formatted byte limit:The image must be smaller than ${maxBytesLabel}:SIZE:.`;
  }

  return '';
}
