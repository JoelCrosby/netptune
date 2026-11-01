import { docsNav } from '~/data/docs-nav';
import { extractHeadings, parseDocument, type DocHeading } from './docs-content';

export type DocSearchResult = {
  href: string;
  title: string;
  context: string;
};

type DocSearchEntry = {
  href: string;
  title: string;
  description: string;
  section: string;
  headings: DocHeading[];
};

const sources = import.meta.glob<string>('../content/docs/**/*.md', {
  query: '?raw',
  import: 'default',
  eager: true,
});

function hrefFor(path: string): string {
  const slug = path
    .replace('../content/docs', '')
    .replace(/\.md$/, '')
    .replace(/\/?index$/, '');
  return `/docs${slug}`;
}

const sourcesByHref = new Map(
  Object.entries(sources).map(([path, source]) => [hrefFor(path), source]),
);

const entries: DocSearchEntry[] = docsNav.flatMap((section) => {
  return section.items.flatMap((item) => {
    const source = sourcesByHref.get(item.href);

    if (!source) {
      return [];
    }

    const { frontmatter, content } = parseDocument(source);

    return [
      {
        href: item.href,
        title: frontmatter.title,
        description: frontmatter.description ?? '',
        section: section.heading,
        headings: extractHeadings(content),
      },
    ];
  });
});

function matches(haystack: string, terms: string[]): boolean {
  const lower = haystack.toLowerCase();
  return terms.every((term) => lower.includes(term));
}

export function searchDocs(query: string, limit = 8): DocSearchResult[] {
  const terms = query.toLowerCase().split(/\s+/).filter(Boolean);

  if (terms.length === 0) {
    return [];
  }

  const pages = entries
    .filter((entry) => matches(`${entry.title} ${entry.description}`, terms))
    .map((entry) => ({ href: entry.href, title: entry.title, context: entry.section }));

  const sections = entries.flatMap((entry) => {
    return entry.headings
      .filter((heading) => matches(heading.label, terms))
      .map((heading) => {
        return { href: `${entry.href}#${heading.id}`, title: heading.label, context: entry.title };
      });
  });

  return [...pages, ...sections].slice(0, limit);
}
