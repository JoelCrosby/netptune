import slugify from '@sindresorhus/slugify';
import { appLocale } from '@core/util/locale';

export const toUrlSlug = (value: string) => slugify(value);

export function toLowerText(value: string): string {
  return value.toLowerCase();
}

const listFormats = {
  and: new Intl.ListFormat(appLocale, { type: 'conjunction' }),
  or: new Intl.ListFormat(appLocale, { type: 'disjunction' }),
};

export function joinNaturalList(
  values: string[],
  conjunction: 'and' | 'or' = 'and'
): string {
  return listFormats[conjunction].format(values);
}

export type TextKeys<T> = {
  [K in keyof T]: T[K] extends string | null | undefined ? K : never;
}[keyof T];

export function matchesQuery<T>(
  item: T,
  query: string,
  keys: readonly TextKeys<T>[]
): boolean {
  const normalizedQuery = query.trim().toLowerCase();

  if (!normalizedQuery) {
    return true;
  }

  return keys.some((key) => {
    const value = item[key] as string | null | undefined;
    return (value ?? '').toLowerCase().includes(normalizedQuery);
  });
}
