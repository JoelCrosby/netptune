import type { JSX } from 'solid-js';
import { children, For, Index } from 'solid-js';
import type { FeatureHighlight } from '~/types/feature-highlight';

export const featureHighlights: FeatureHighlight[] = [
  {
    eyebrow: 'Kanban boards',
    title: 'The way your team works — visualized.',
    description:
      "Drag tasks between columns, assign them to teammates, and watch the board update for everyone in real time. No refreshing. No confusion about who's doing what.",
    extra: () => (
      <p class="text-sm leading-relaxed text-slate-500 dark:text-white/55">
        Custom columns, drag-and-drop reordering, multiple boards per project. Structure your
        workflow exactly how your team thinks.
      </p>
    ),
    visual: () => (
      <picture>
        <div data-src="/images/kanban-boards-dark.webp" />
        <div data-src="/images/kanban-boards-light.webp" />

        <img
          width={580}
          height={290}
          class="mt-6 rounded-lg border border-neutral-200 object-contain shadow-2xl shadow-brand/60 inset-ring-brand dark:border-white/15"
          src="/images/kanban-boards-light.webp"
          alt="Screenshot of the Netptune kanbanboards"
        />
      </picture>
    ),
  },
  {
    eyebrow: 'Workspaces',
    title: 'One workspace. Every project.',
    description:
      'Bring your entire team under one roof. Create projects, assign members, and keep work organized across as many boards as you need — all within a shared workspace.',
    extra: () => (
      <p class="text-sm leading-relaxed text-slate-500 dark:text-white/55">
        Separate workspaces for different teams or clients. Role-based access ensures everyone sees
        what they should — and nothing they shouldn't.
      </p>
    ),
    visual: () => <WorkspaceVisual />,
    reversed: true,
  },
  {
    eyebrow: 'Audit logs',
    title: 'A complete record of everything.',
    description:
      'Every change — who made it, what changed, and when — is logged automatically. Task created? Logged. Status changed? Logged. Reassigned? Logged.',
    extra: () => (
      <div class="mt-4">
        <p class="mb-3 text-sm font-semibold text-slate-700 dark:text-white/80">
          11 tracked activity types:
        </p>
        <div class="flex flex-wrap gap-2">
          <For
            each={[
              'Create',
              'Modify',
              'Delete',
              'Assign',
              'Move',
              'Reorder',
              'Flag',
              'Unflag',
              'Rename',
              'Update description',
              'Change status',
            ]}
          >
            {(t) => (
              <span class="rounded-md bg-slate-100 px-2.5 py-1 text-xs font-medium text-slate-600 dark:bg-white/10 dark:text-white/60">
                {t}
              </span>
            )}
          </For>
        </div>
      </div>
    ),
    visual: () => <ActivityLogVisual />,
  },
];

function WorkspaceVisual() {
  const projects = [
    { name: 'Website Redesign', boards: 4, members: 5, color: 'bg-violet-500' },
    { name: 'API v2', boards: 6, members: 3, color: 'bg-brand' },
    { name: 'Mobile App', boards: 3, members: 8, color: 'bg-sky-500' },
    { name: 'Marketing Q3', boards: 2, members: 4, color: 'bg-amber-500' },
  ];

  return (
    <WindowCard>
      <p class="mb-3 text-xs font-medium tracking-wider text-neutral-500 uppercase">
        Acme Corp &mdash; Workspace
      </p>
      <div class="space-y-2">
        <For each={projects}>
          {(p) => (
            <div class="flex items-center gap-3 rounded-lg bg-neutral-50 p-3 transition-colors hover:bg-neutral-200/60 dark:bg-neutral-800 dark:hover:bg-neutral-700/60">
              <div
                class={`h-8 w-8 rounded-lg ${p.color} flex shrink-0 items-center justify-center`}
              >
                <span class="text-[11px] font-bold text-white">{p.name[0]}</span>
              </div>
              <div class="min-w-0 flex-1">
                <p class="truncate text-xs font-medium text-neutral-800 dark:text-neutral-200">
                  {p.name}
                </p>
                <p class="text-[11px] text-neutral-500">{p.boards} boards</p>
              </div>
              <div class="flex -space-x-1">
                <Index each={Array.from({ length: Math.min(p.members, 3) })}>
                  {(_item, i) => (
                    <div
                      class="flex h-5 w-5 items-center justify-center rounded-full border border-neutral-300 bg-neutral-100 dark:border-neutral-900 dark:bg-neutral-600"
                      style={{ 'z-index': 3 - i }}
                    >
                      <div class="h-2 w-2 rounded-full bg-neutral-400" />
                    </div>
                  )}
                </Index>
                {p.members > 3 && (
                  <div class="flex h-5 w-5 items-center justify-center rounded-full border border-neutral-300 bg-neutral-200 dark:border-neutral-900 dark:bg-neutral-700">
                    <span class="text-[9px] text-neutral-400">+{p.members - 3}</span>
                  </div>
                )}
              </div>
            </div>
          )}
        </For>
      </div>
    </WindowCard>
  );
}

function ActivityLogVisual() {
  const entries = [
    {
      type: 'Create',
      user: 'Sarah',
      task: 'Add OAuth integration',
      time: 'just now',
      color: 'text-violet-400',
    },
    {
      type: 'Assign',
      user: 'Marcus',
      task: 'Real-time sync via SSE',
      time: '2m ago',
      color: 'text-sky-400',
    },
    {
      type: 'Move',
      user: 'Sarah',
      task: 'API rate limiting',
      time: '5m ago',
      color: 'text-violet-400',
    },
    {
      type: 'Flag',
      user: 'Priya',
      task: 'Fix memory leak',
      time: '12m ago',
      color: 'text-amber-400',
    },
    {
      type: 'Modify',
      user: 'Marcus',
      task: 'Deployment checklist',
      time: '18m ago',
      color: 'text-neutral-400',
    },
  ];

  return (
    <WindowCard>
      <p class="mb-3 text-xs font-medium tracking-wider text-neutral-500 uppercase">Activity log</p>
      <div class="space-y-1">
        <For each={entries}>
          {(e) => (
            <div class="flex items-start gap-3 rounded-lg px-3 py-2.5 transition-colors hover:bg-neutral-100 dark:hover:bg-neutral-800">
              <span
                class={`mt-0.5 w-14 shrink-0 text-[10px] font-bold tracking-wider uppercase ${e.color}`}
              >
                {e.type}
              </span>
              <div class="min-w-0 flex-1">
                <p class="text-[11px] leading-snug text-neutral-300">
                  <span class="font-medium text-neutral-100">{e.user}</span>
                  {' · '}
                  <span class="truncate">{e.task}</span>
                </p>
              </div>
              <span class="shrink-0 text-[10px] text-neutral-600">{e.time}</span>
            </div>
          )}
        </For>
      </div>
    </WindowCard>
  );
}

function WindowControls() {
  return (
    <div class="mb-5 flex items-center gap-1.5">
      <div class="h-2.5 w-2.5 rounded-full bg-red-500/70" />
      <div class="h-2.5 w-2.5 rounded-full bg-yellow-500/70" />
      <div class="h-2.5 w-2.5 rounded-full bg-green-500/70" />
    </div>
  );
}

function WindowCard(props: { children: JSX.Element }) {
  const resolved = children(() => props.children);

  return (
    <div class="rounded-2xl border border-neutral-200 bg-white p-5 shadow-2xl select-none dark:border-neutral-700 dark:bg-neutral-900 dark:ring-neutral-800">
      {WindowControls()}
      {resolved()}
    </div>
  );
}
