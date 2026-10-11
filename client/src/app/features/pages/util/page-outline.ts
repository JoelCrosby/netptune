import { OutlineItem } from '@static/components/outline-list/outline-list.component';

const headingPattern = /^(#{1,6})\s+(.+?)\s*#*\s*$/;
const fencePattern = /^\s*(```|~~~)/;

// Headings in document order; ids are their index, which is what the editor scrolls by.
export function outlineFromMarkdown(markdown: string | null): OutlineItem[] {
  if (!markdown) return [];

  const items: OutlineItem[] = [];
  let insideFence = false;

  for (const line of markdown.split('\n')) {
    const isFence = fencePattern.test(line);

    if (isFence) {
      insideFence = !insideFence;
      continue;
    }

    if (insideFence) continue;

    const match = headingPattern.exec(line);

    if (!match) continue;

    items.push({
      id: items.length,
      level: match[1].length,
      label: stripInlineMarkdown(match[2]),
    });
  }

  return items;
}

function stripInlineMarkdown(text: string): string {
  return text
    .replace(/!?\[([^\]]*)\]\([^)]*\)/g, '$1')
    .replace(/[*_`~]/g, '')
    .trim();
}
