import { SolidMarkdown } from 'solid-markdown';
import remarkDirective from 'remark-directive';
import remarkGfm from 'remark-gfm';
import remarkDocDirectives from '~/lib/remark-doc-directives';
import DocLayout from './DocLayout';
import DocPagination from './DocPagination';
import './markdown.css';

type NavLink = { href: string; label: string };

type MarkdownDocProps = {
  source: string;
  prev?: NavLink;
  next?: NavLink;
};

type DocFrontmatter = {
  title: string;
  description?: string;
};

function parseDocument(source: string): { frontmatter: DocFrontmatter; content: string } {
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

export default function MarkdownDoc(props: MarkdownDocProps) {
  const document = () => parseDocument(props.source);

  return (
    <DocLayout
      title={document().frontmatter.title}
      description={document().frontmatter.description}
    >
      <SolidMarkdown
        class="docs-markdown"
        remarkPlugins={[remarkGfm, remarkDirective, remarkDocDirectives]}
        children={document().content}
      />
      <DocPagination prev={props.prev} next={props.next} />
    </DocLayout>
  );
}
