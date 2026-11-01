import { Eye, FileLock2, History, Wallet } from 'lucide-solid';
import type { Feature } from '~/types/feature';
import type { FeatureHighlight } from '~/types/feature-highlight';

function DocsLink(props: { href: string; label: string }) {
  return (
    <a
      href={props.href}
      class="inline-block text-sm font-semibold text-brand underline decoration-violet-300 underline-offset-4 transition-colors hover:text-brand-dark dark:text-violet-300 dark:decoration-violet-500/50 dark:hover:text-fuchsia-300"
    >
      {props.label}
    </a>
  );
}

function Screenshot(props: { name: string; alt: string }) {
  return (
    <div class="min-w-0 flex-1">
      <img
        width={1600}
        height={900}
        loading="lazy"
        class="rounded-lg border border-neutral-200 object-contain shadow-2xl shadow-brand/30 dark:hidden"
        src={`/images/${props.name}-light.webp`}
        alt={props.alt}
      />
      <img
        width={1600}
        height={900}
        loading="lazy"
        class="hidden rounded-lg border border-white/15 object-contain shadow-2xl shadow-brand/30 dark:block"
        src={`/images/${props.name}-dark.webp`}
        alt={props.alt}
      />
    </div>
  );
}

export const assistantPrompts: string[] = [
  'What slipped last sprint?',
  'Who has too much on this week?',
  'Turn these notes into tasks',
  'Write acceptance criteria for this',
  'Is this task ready to start?',
  'Tidy up the backlog tags',
];

export const assistantShowcase: FeatureHighlight[] = [
  {
    eyebrow: 'Ask',
    title: 'Ask the way you would ask a teammate.',
    description:
      "Wondering how the sprint went, what's blocked, or what the onboarding page says? Just ask. The assistant looks it up across your workspace and gives you a straight answer, with the numbers when it matters.",
    extra: () => <DocsLink href="/docs/assistant/asking" label="What you can ask" />,
    visual: () => (
      <Screenshot name="assistant" alt="The Netptune assistant answering a question about a sprint" />
    ),
  },
  {
    eyebrow: 'Approve',
    title: 'It suggests. You decide.',
    description:
      "Ask for a change and the assistant lays out exactly what it would do. Keep the bits you like, tweak the ones you don't, and apply. Changed your mind later? Undo it.",
    reversed: true,
    extra: () => <DocsLink href="/docs/assistant/proposals" label="How reviewing works" />,
    visual: () => (
      <Screenshot
        name="assistant-proposals"
        alt="Reviewing the assistant's proposed changes before applying them"
      />
    ),
  },
  {
    eyebrow: 'Write',
    title: 'A writing partner right where you type.',
    description:
      'Type /ai in a task or a page and ask for a checklist, a summary or the next paragraph. It writes in place, and one undo takes it all back out.',
    extra: () => <DocsLink href="/docs/assistant/writing" label="Writing with the assistant" />,
    visual: () => (
      <Screenshot name="assistant-writing" alt="The assistant tightening a selected paragraph on a page" />
    ),
  },
  {
    eyebrow: 'Review',
    title: 'Catch the gaps before work starts.',
    description:
      'Ask for a review and the assistant reads the task like a fresh pair of eyes, pointing out missing details, fuzzy wording and work that overlaps. Take the suggestions you like and dismiss the rest.',
    reversed: true,
    extra: () => <DocsLink href="/docs/assistant/task-review" label="How task reviews work" />,
    visual: () => (
      <Screenshot name="task-review" alt="A task review with its verdict and a scope finding" />
    ),
  },
  {
    eyebrow: 'Automate',
    title: 'Describe the rule. It drafts it.',
    description:
      "Say what should happen and when, in plain words. The assistant drafts the rule for you to check, and it stays switched off until you're happy with it.",
    extra: () => (
      <DocsLink href="/docs/assistant/proposals" label="Automations with the assistant" />
    ),
    visual: () => (
      <Screenshot name="assistant-automation" alt="An automation rule drafted by the assistant, ready to review" />
    ),
  },
  {
    eyebrow: 'Research',
    title: 'It knows your code, and the web.',
    description:
      'Link a repository and the assistant can look through the code and pull requests behind a task. Connect web search and it can check the outside world too.',
    reversed: true,
    extra: () => <DocsLink href="/docs/assistant/asking" label="Code and the web" />,
    visual: () => (
      <Screenshot name="assistant-research" alt="The assistant reading a web page and comparing it with a plan" />
    ),
  },
];

export const assistantTrust: Feature[] = [
  {
    icon: Eye,
    title: 'Sees only what you see',
    description: "It works with your access, so it can't wander anywhere you couldn't.",
  },
  {
    icon: FileLock2,
    title: 'Leaves your files alone',
    description: "It can tell you a file is there, but it never opens it.",
  },
  {
    icon: History,
    title: 'Everything is on the record',
    description: 'Changes it helps with show up in your history, clearly marked.',
  },
  {
    icon: Wallet,
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
