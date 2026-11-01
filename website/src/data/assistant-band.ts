export interface AssistantBandItem {
  title: string;
  description: string;
  href: string;
}

export interface AssistantProposal {
  id: string;
  text: string;
  change: string;
  defaultAccepted: boolean;
}

export const assistantBandItems: AssistantBandItem[] = [
  {
    title: 'Ask your workspace',
    description: '"What slipped last sprint?" gets a real answer, not a shrug.',
    href: '/docs/assistant/asking',
  },
  {
    title: 'Nothing changes until you say so',
    description: 'It suggests, you pick what goes in, and you can always undo it.',
    href: '/docs/assistant/proposals',
  },
  {
    title: 'Write in place',
    description: 'Type /ai in a task or a page and it picks up right where you are.',
    href: '/docs/assistant/writing',
  },
  {
    title: 'Review tasks before work starts',
    description: 'A fresh pair of eyes that spots the gaps while they are cheap to fix.',
    href: '/docs/assistant/task-review',
  },
  {
    title: 'Build automations by asking',
    description: 'Describe the rule in plain words and it drafts it for you.',
    href: '/docs/assistant/proposals',
  },
  {
    title: 'Sees only what you see',
    description: 'It works with your access, and you can always see what it costs.',
    href: '/docs/assistant/access-and-cost',
  },
];

export const assistantProposals: AssistantProposal[] = [
  {
    id: 'NP-198',
    text: 'Rate-limit the export endpoint',
    change: 'Sprint 13 → 14',
    defaultAccepted: true,
  },
  {
    id: 'NP-204',
    text: 'Calendar drag loses timezone',
    change: 'Sprint 13 → 14',
    defaultAccepted: true,
  },
  {
    id: 'NP-211',
    text: 'Onboarding checklist copy',
    change: 'Sprint 13 → Backlog',
    defaultAccepted: false,
  },
];
