import MarkdownDoc from '~/components/docs/MarkdownDoc';
import source from '~/content/docs/automations.md?raw';

export default function AutomationsPage() {
  return <MarkdownDoc source={source} />;
}
