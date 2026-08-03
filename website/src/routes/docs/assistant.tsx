import MarkdownDoc from '~/components/docs/MarkdownDoc';
import source from '~/content/docs/assistant.md?raw';

export default function AssistantPage() {
  return (
    <MarkdownDoc
      source={source}
      prev={{ href: '/docs', label: 'Overview' }}
      next={{ href: '/docs/docker-compose', label: 'Docker Compose' }}
    />
  );
}
