import { Bell, CalendarRange, Flag, Workflow, Zap } from 'lucide-solid';
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
      <div class="min-w-0 flex-1">
        <img
          width={580}
          height={290}
          class="mt-6 rounded-lg border border-neutral-200 object-contain shadow-2xl shadow-brand/60 inset-ring-brand dark:hidden"
          src="/images/kanban-boards-light.webp"
          alt="Screenshot of the Netptune kanban boards"
        />
        <img
          width={580}
          height={290}
          class="mt-6 hidden rounded-lg border border-white/15 object-contain shadow-2xl shadow-brand/60 inset-ring-brand dark:block"
          src="/images/kanban-boards-dark.webp"
          alt="Screenshot of the Netptune kanban boards"
        />
      </div>
    ),
  },
  {
    eyebrow: 'Automations',
    title: 'Set the rules. Skip the busywork.',
    description:
      'Build trigger-based rules that react when a task changes status, has fields updated, or sits unassigned for too long — then notify assignees, flag the task, or update it automatically.',
    reversed: true,
    extra: () => (
      <p class="text-sm leading-relaxed text-slate-500 dark:text-white/55">
        Every run is recorded, so you can see exactly which rules fired, on which tasks, and what
        they changed.
      </p>
    ),
    visual: () => (
      <div class="w-full max-w-md flex-1 rounded-xl border border-slate-200 bg-white p-6 shadow-2xl shadow-brand/30 dark:border-white/15 dark:bg-[#0a0a0a]">
        <div class="mb-4 flex items-center gap-3">
          <div class="flex h-9 w-9 items-center justify-center rounded-lg bg-violet-50 text-brand dark:bg-violet-500/10">
            <Workflow size={18} />
          </div>
          <p class="text-sm font-semibold text-slate-900 dark:text-white">Stale task follow-up</p>
        </div>

        <div class="space-y-2">
          <div class="rounded-lg border border-slate-200 p-3 dark:border-white/10">
            <p class="mb-1 text-[11px] font-semibold tracking-wider text-slate-400 uppercase dark:text-white/35">
              When
            </p>
            <div class="flex items-center gap-2 text-sm text-slate-700 dark:text-white/70">
              <Zap size={14} class="text-brand" />
              Task is unassigned for 3 days
            </div>
          </div>

          <div class="rounded-lg border border-slate-200 p-3 dark:border-white/10">
            <p class="mb-1 text-[11px] font-semibold tracking-wider text-slate-400 uppercase dark:text-white/35">
              Then
            </p>
            <div class="flex items-center gap-2 text-sm text-slate-700 dark:text-white/70">
              <Bell size={14} class="text-brand" />
              Notify board members
            </div>
            <div class="mt-2 flex items-center gap-2 text-sm text-slate-700 dark:text-white/70">
              <Flag size={14} class="text-brand" />
              Flag task as “Needs owner”
            </div>
          </div>
        </div>
      </div>
    ),
  },
  {
    eyebrow: 'Sprints',
    title: 'Plan in time-boxes. Ship on schedule.',
    description:
      'Group tasks into time-boxed sprints, start and complete them as a unit, and track progress with live task counts and status breakdowns.',
    visual: () => (
      <div class="w-full max-w-md flex-1 rounded-xl border border-slate-200 bg-white p-6 shadow-2xl shadow-brand/30 dark:border-white/15 dark:bg-[#0a0a0a]">
        <div class="mb-4 flex items-center justify-between">
          <div class="flex items-center gap-3">
            <div class="flex h-9 w-9 items-center justify-center rounded-lg bg-violet-50 text-brand dark:bg-violet-500/10">
              <CalendarRange size={18} />
            </div>
            <div>
              <p class="text-sm font-semibold text-slate-900 dark:text-white">Sprint 14</p>
              <p class="text-xs text-slate-400 dark:text-white/35">Jun 2 – Jun 13</p>
            </div>
          </div>
          <span class="rounded-full bg-emerald-50 px-2.5 py-1 text-xs font-medium text-emerald-600 dark:bg-emerald-500/10 dark:text-emerald-400">
            Active
          </span>
        </div>

        <div class="mb-2 flex items-center justify-between text-xs text-slate-400 dark:text-white/35">
          <span>18 of 24 tasks done</span>
          <span>75%</span>
        </div>
        <div class="mb-4 h-2 overflow-hidden rounded-full bg-slate-100 dark:bg-white/10">
          <div class="h-full w-3/4 rounded-full bg-brand" />
        </div>

        <div class="grid grid-cols-3 gap-2 text-center">
          <div class="rounded-lg border border-slate-200 py-2 dark:border-white/10">
            <p class="text-sm font-semibold text-slate-900 dark:text-white">18</p>
            <p class="text-[11px] text-slate-400 dark:text-white/35">Done</p>
          </div>
          <div class="rounded-lg border border-slate-200 py-2 dark:border-white/10">
            <p class="text-sm font-semibold text-slate-900 dark:text-white">4</p>
            <p class="text-[11px] text-slate-400 dark:text-white/35">In progress</p>
          </div>
          <div class="rounded-lg border border-slate-200 py-2 dark:border-white/10">
            <p class="text-sm font-semibold text-slate-900 dark:text-white">2</p>
            <p class="text-[11px] text-slate-400 dark:text-white/35">To do</p>
          </div>
        </div>
      </div>
    ),
  },
];
