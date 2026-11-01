import MarkdownDoc from '~/components/docs/MarkdownDoc';
import source from '~/content/docs/assistant/proposals.md?raw';

export default function AssistantProposalsPage() {
  return <MarkdownDoc source={source} />;
}
