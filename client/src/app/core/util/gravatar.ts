// Gravatar accepts SHA-256 email hashes, which SubtleCrypto can produce without an MD5 dependency.
export async function gravatarUrl(email: string, size = 512) {
  const normalised = email.trim().toLowerCase();
  const bytes = new TextEncoder().encode(normalised);
  const digest = await crypto.subtle.digest('SHA-256', bytes);
  const hash = Array.from(new Uint8Array(digest))
    .map((byte) => byte.toString(16).padStart(2, '0'))
    .join('');

  return `https://gravatar.com/avatar/${hash}?s=${size}&d=404`;
}

// With d=404 an address without a Gravatar fails to load instead of returning a placeholder.
export function imageLoads(url: string) {
  return new Promise<boolean>((resolve) => {
    const image = new Image();

    image.onload = () => resolve(true);
    image.onerror = () => resolve(false);
    image.src = url;
  });
}
