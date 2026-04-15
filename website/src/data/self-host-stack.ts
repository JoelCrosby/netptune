import { Database, Cpu, Workflow, Archive, Layers } from 'lucide-solid';
import type { StackItem } from '~/types/stack-item';

export const selfHostStack: StackItem[] = [
  {
    icon: Database,
    label: 'PostgreSQL',
    sublabel: 'Persistence',
  },
  {
    icon: Cpu,
    label: 'Redis',
    sublabel: 'Caching',
  },
  {
    icon: Workflow,
    label: 'NATS JetStream',
    sublabel: 'Background jobs',
  },
  {
    icon: Archive,
    label: 'S3-compatible',
    sublabel: 'File storage',
  },
  {
    icon: Layers,
    label: 'Helm / Kubernetes',
    sublabel: 'Deployment',
  },
];
