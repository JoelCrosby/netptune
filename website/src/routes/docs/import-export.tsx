import MarkdownDoc from '~/components/docs/MarkdownDoc';
import source from '~/content/docs/import-export.md?raw';

export default function ImportExportPage() {
  return <MarkdownDoc source={source} />;
}
