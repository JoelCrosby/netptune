import MarkdownDoc from '~/components/docs/MarkdownDoc';
import source from '~/content/docs/pages.md?raw';

export default function PagesDocPage() {
  return <MarkdownDoc source={source} />;
}
