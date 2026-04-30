import slugify from '@sindresorhus/slugify';

export const toUrlSlug = (value: string) => slugify(value);
