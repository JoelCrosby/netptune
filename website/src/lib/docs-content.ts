export type DocFrontmatter = {
  title: string;
  description?: string;
};

export type DocHeading = { id: string; label: string };

export function parseDocument(source: string): { frontmatter: DocFrontmatter; content: string } {
  const match = source.match(/^---\n([\s\S]*?)\n---\n?([\s\S]*)$/);

  if (!match) {
    throw new Error('Documentation Markdown must begin with front matter');
  }

  const values = Object.fromEntries(
    match[1].split('\n').map((line) => {
      const separator = line.indexOf(':');
      const key = line.slice(0, separator).trim();
      const rawValue = line.slice(separator + 1).trim();
      const value =
        (rawValue.startsWith('"') && rawValue.endsWith('"')) ||
        (rawValue.startsWith("'") && rawValue.endsWith("'"))
          ? rawValue.slice(1, -1)
          : rawValue;
      return [key, value];
    }),
  );

  if (!values.title) {
    throw new Error('Documentation front matter must include a title');
  }

  return {
    frontmatter: values as DocFrontmatter,
    content: match[2].trim(),
  };
}

export function slugify(text: string): string {
  return text
    .toLowerCase()
    .replace(/[^a-z0-9\s-]/g, '')
    .trim()
    .replace(/\s+/g, '-');
}

function stripInlineMarkdown(text: string): string {
  return text.replace(/\[([^\]]+)\]\([^)]*\)/g, '$1').replace(/[*_`]/g, '');
}

export function extractHeadings(content: string, depth = 2): DocHeading[] {
  const prefix = `${'#'.repeat(depth)} `;
  const headings: DocHeading[] = [];
  let inFence = false;

  for (const line of content.split('\n')) {
    if (line.startsWith('```')) {
      inFence = !inFence;
      continue;
    }

    if (inFence || !line.startsWith(prefix)) {
      continue;
    }

    const label = stripInlineMarkdown(line.slice(prefix.length).trim());
    headings.push({ id: slugify(label), label });
  }

  return headings;
}

export function readingTime(content: string): string {
  const words = content.split(/\s+/).filter(Boolean).length;
  const minutes = Math.max(1, Math.round(words / 200));
  return `${minutes} min read`;
}
