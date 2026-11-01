export interface AssistantShowcaseItem {
  eyebrow: string;
  title: string;
  description: string;
  link: { href: string; label: string };
  image: string;
  alt: string;
}

export interface AssistantTrustItem {
  title: string;
  description: string;
}

export const assistantPrompts: string[] = [
  'What slipped last sprint?',
  'Who has too much on this week?',
  'Turn these notes into tasks',
  'Write acceptance criteria for this',
  'Is this task ready to start?',
  'Tidy up the backlog tags',
];

export const assistantShowcase: AssistantShowcaseItem[] = [
  {
    eyebrow: 'Ask',
    title: 'Ask the way you would ask a teammate.',
    description:
      "Wondering how the sprint went, what's blocked, or what the onboarding page says? Just ask. The assistant looks it up across your workspace and gives you a straight answer, with the numbers when it matters.",
    link: { href: '/docs/assistant/asking', label: 'What you can ask' },
    image: '/images/assistant-light.webp',
    alt: 'The Netptune assistant answering a question about a sprint',
  },
  {
    eyebrow: 'Approve',
    title: 'It suggests. You decide.',
    description:
      "Ask for a change and the assistant lays out exactly what it would do. Keep the bits you like, tweak the ones you don't, and apply. Changed your mind later? Undo it.",
    link: { href: '/docs/assistant/proposals', label: 'How reviewing works' },
    image: '/images/assistant-proposals-light.webp',
    alt: "Reviewing the assistant's proposed changes before applying them",
  },
  {
    eyebrow: 'Write',
    title: 'A writing partner right where you type.',
    description:
      'Type /ai in a task or a page and ask for a checklist, a summary or the next paragraph. It writes in place, and one undo takes it all back out.',
    link: { href: '/docs/assistant/writing', label: 'Writing with the assistant' },
    image: '/images/assistant-writing-light.webp',
    alt: 'The assistant tightening a selected paragraph on a page',
  },
  {
    eyebrow: 'Review',
    title: 'Catch the gaps before work starts.',
    description:
      'Ask for a review and the assistant reads the task like a fresh pair of eyes, pointing out missing details, fuzzy wording and work that overlaps. Take the suggestions you like and dismiss the rest.',
    link: { href: '/docs/assistant/task-review', label: 'How task reviews work' },
    image: '/images/task-review-light.webp',
    alt: 'A task review with its verdict and a scope finding',
  },
  {
    eyebrow: 'Automate',
    title: 'Describe the rule. It drafts it.',
    description:
      "Say what should happen and when, in plain words. The assistant drafts the rule for you to check, and it stays switched off until you're happy with it.",
    link: { href: '/docs/assistant/proposals', label: 'Automations with the assistant' },
    image: '/images/assistant-automation-light.webp',
    alt: 'An automation rule drafted by the assistant, ready to review',
  },
  {
    eyebrow: 'Research',
    title: 'It knows your code, and the web.',
    description:
      'Link a repository and the assistant can look through the code and pull requests behind a task. Connect web search and it can check the outside world too.',
    link: { href: '/docs/assistant/asking', label: 'Code and the web' },
    image: '/images/assistant-research-light.webp',
    alt: 'The assistant reading a web page and comparing it with a plan',
  },
];

export const assistantTrust: AssistantTrustItem[] = [
  {
    title: 'Sees only what you see',
    description: "It works with your access, so it can't wander anywhere you couldn't.",
  },
  {
    title: 'Leaves your files alone',
    description: 'It can tell you a file is there, but it never opens it.',
  },
  {
    title: 'Everything is on the record',
    description: 'Changes it helps with show up in your history, clearly marked.',
  },
  {
    title: 'No surprise bills',
    description: 'Bring your own key, see what each chat costs, and set a monthly cap.',
  },
];

export const assistantHelpsWith: string[] = [
  'Tasks',
  'Sprints',
  'Boards',
  'Pages',
  'Comments',
  'Projects',
  'Statuses and tags',
  'Automations',
  'Reports',
  'Your code',
  'The web',
];
