import MarkdownDoc from '~/components/docs/MarkdownDoc';
import source from '~/content/docs/assistant/index.md?raw';

export default function AssistantPage() {
  return <MarkdownDoc source={source} />;
}
