import MarkdownDoc from '~/components/docs/MarkdownDoc';
import source from '~/content/docs/source-control.md?raw';

export default function SourceControlPage() {
  return (
    <MarkdownDoc
      source={source}
      prev={{ href: '/docs/pages', label: 'Pages' }}
      next={{ href: '/docs/queries', label: 'Queries' }}
    />
  );
}
