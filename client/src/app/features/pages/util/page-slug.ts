export function isSamePageSlug(slug: string, other: string): boolean {
  return idOf(slug) === idOf(other);
}

function idOf(slug: string): string {
  return slug.slice(slug.lastIndexOf('-') + 1);
}
