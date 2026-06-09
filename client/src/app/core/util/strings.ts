import slugify from '@sindresorhus/slugify';

export const toUrlSlug = (value: string) => slugify(value);

export function toLowerText(value: string): string {
  return value.toLowerCase();
}

export function joinNaturalList(
  values: string[],
  conjunction: 'and' | 'or' = 'and'
): string {
  if (values.length === 0) return '';
  if (values.length === 1) return values[0];
  if (values.length === 2) return `${values[0]} ${conjunction} ${values[1]}`;

  return `${values.slice(0, -1).join(', ')} ${conjunction} ${values.at(-1)}`;
}
