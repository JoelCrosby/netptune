import { Database, Cpu, Workflow, Archive, Layers, Search } from 'lucide-solid';
import type { StackItem } from '~/types/stack-item';

export const selfHostStack: StackItem[] = [
  {
    icon: Database,
    label: 'PostgreSQL',
    sublabel: 'Persistence',
  },
  {
    icon: Cpu,
    label: 'Valkey',
    sublabel: 'Caching',
  },
  {
    icon: Workflow,
    label: 'NATS JetStream',
    sublabel: 'Event messaging',
  },
  {
    icon: Search,
    label: 'Meilisearch',
    sublabel: 'Workspace search',
  },
  {
    icon: Archive,
    label: 'AWS S3',
    sublabel: 'Attachments and audit archives',
  },
  {
    icon: Layers,
    label: 'Helm / Kubernetes',
    sublabel: 'Deployment',
  },
];
