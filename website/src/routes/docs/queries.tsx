import MarkdownDoc from '~/components/docs/MarkdownDoc';
import source from '~/content/docs/queries.md?raw';

export default function QueriesPage() {
  return <MarkdownDoc source={source} />;
}
