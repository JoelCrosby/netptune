import { SolidMarkdown } from 'solid-markdown';
import remarkDirective from 'remark-directive';
import remarkGfm from 'remark-gfm';
import { extractHeadings, parseDocument, readingTime } from '~/lib/docs-content';
import remarkDocDirectives from '~/lib/remark-doc-directives';
import DocLayout from './DocLayout';
import './markdown.css';

type MarkdownDocProps = {
  source: string;
};

export default function MarkdownDoc(props: MarkdownDocProps) {
  const document = () => parseDocument(props.source);

  return (
    <DocLayout
      title={document().frontmatter.title}
      description={document().frontmatter.description}
      readingTime={readingTime(document().content)}
      headings={extractHeadings(document().content)}
    >
      <SolidMarkdown
        class="docs-markdown"
        remarkPlugins={[remarkGfm, remarkDirective, remarkDocDirectives]}
        children={document().content}
      />
    </DocLayout>
  );
}
