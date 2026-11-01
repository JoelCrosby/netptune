import MarkdownDoc from '~/components/docs/MarkdownDoc';
import source from '~/content/docs/assistant/writing.md?raw';

export default function AssistantWritingPage() {
  return <MarkdownDoc source={source} />;
}
