import slugify from 'slugify';

export const toUrlSlug = (value: string) =>
  slugify(value, {
    replacement: '-',
    remove: undefined,
    lower: true,
    strict: true,
    locale: 'en',
  });
