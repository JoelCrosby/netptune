export function pathOf(url: string): string {
  return url.split(/[?#]/)[0];
}

export function queryOf(url: string): string {
  const [withoutFragment] = url.split('#');

  return withoutFragment.split('?')[1] ?? '';
}
