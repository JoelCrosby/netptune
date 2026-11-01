import { ClipboardCheck, MessageCircleQuestion, PenLine, ShieldCheck, ThumbsUp, Workflow } from 'lucide-solid';
import type { IconComponent } from '~/types/icon';

export interface AssistantBandItem {
  icon: IconComponent;
  title: string;
  description: string;
  href: string;
}

export const assistantBandItems: AssistantBandItem[] = [
  {
    icon: MessageCircleQuestion,
    title: 'Ask your workspace',
    description: '"What slipped last sprint?" gets a real answer, not a shrug.',
    href: '/docs/assistant/asking',
  },
  {
    icon: ThumbsUp,
    title: 'Nothing changes until you say so',
    description: 'It suggests, you pick what goes in, and you can always undo it.',
    href: '/docs/assistant/proposals',
  },
  {
    icon: PenLine,
    title: 'Write in place',
    description: 'Type /ai in a task or a page and it picks up right where you are.',
    href: '/docs/assistant/writing',
  },
  {
    icon: ClipboardCheck,
    title: 'Review tasks before work starts',
    description: 'A fresh pair of eyes that spots the gaps while they are cheap to fix.',
    href: '/docs/assistant/task-review',
  },
  {
    icon: Workflow,
    title: 'Build automations by asking',
    description: 'Describe the rule in plain words and it drafts it for you.',
    href: '/docs/assistant/proposals',
  },
  {
    icon: ShieldCheck,
    title: 'Sees only what you see',
    description: 'It works with your access, and you can always see what it costs.',
    href: '/docs/assistant/access-and-cost',
  },
];
