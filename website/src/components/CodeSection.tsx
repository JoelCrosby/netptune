import { For } from 'solid-js';

interface HistoryEntry {
  who: string;
  color: string;
  text: string;
  when: string;
}

const history: HistoryEntry[] = [
  { who: 'SK', color: '#673ab7', text: 'Sam moved NP-142 to Review', when: '2m' },
  {
    who: '✦',
    color: '#d946ef',
    text: 'Assistant set estimate to 5 · approved by Priya',
    when: '14m',
  },
  { who: 'GH', color: '#140f22', text: 'PR #318 merged into main', when: '1h' },
  { who: 'AL', color: '#0ea5e9', text: 'Alex edited “Release runbook”', when: '3h' },
];

const reports = ['Throughput', 'Cycle time', 'Burndown', 'Velocity', 'Workload'];

function GitHubCard() {
  return (
    <div class="relative grid gap-10 overflow-hidden rounded-3xl bg-ink p-6 text-white sm:p-10 md:col-span-2 lg:col-span-3 lg:grid-cols-2">
      <div class="pointer-events-none absolute -top-40 -right-30 size-130 rounded-full bg-[radial-gradient(circle,rgb(139_92_246/0.45),transparent_65%)]" />
      <div class="relative flex flex-col justify-center gap-4">
        <span class="font-geist-mono text-xs text-violet-300">GitHub</span>
        <h3 class="m-0 text-[34px] leading-[1.08] font-[650] tracking-[-0.03em] text-balance">
          See the code behind every task.
        </h3>
        <p class="m-0 text-base leading-[1.6] text-pretty text-white/62">
          Put a task ID in a branch, commit or pull request. It links itself to the right task —
          merged or not, checks passing or not.
        </p>
      </div>

      <div class="relative flex min-w-0 flex-col gap-2.5 font-geist-mono text-[13px]">
        <div class="flex items-center gap-2.5 rounded-xl border border-purple-200/12 bg-white/4 px-4 py-3">
          <span class="text-violet-300">⎇</span>
          <span class="truncate text-white/85">
            feat/<span class="text-fuchsia-300">NP-142</span>-saved-board-filters
          </span>
        </div>
        <div class="flex flex-wrap items-center gap-2.5 rounded-xl border border-purple-200/16 bg-white/6 px-4 py-3.5">
          <span class="rounded-full bg-violet-500 px-2.25 py-0.75 text-[11px] text-white">
            Merged
          </span>
          <span class="min-w-45 flex-1 font-geist text-sm text-white">
            #318 Add saved board filters
          </span>
          <span class="text-green-300">✓ 14 checks</span>
        </div>
        <div class="flex flex-wrap items-center gap-2.5 rounded-xl border border-purple-200/12 bg-white/4 px-4 py-3.5">
          <span class="rounded-full bg-green-700 px-2.25 py-0.75 text-[11px] text-white">Open</span>
          <span class="min-w-45 flex-1 font-geist text-sm text-white">
            #321 Persist filter on reload
          </span>
          <span class="text-red-300">✕ 1 failing</span>
        </div>
        <div class="flex items-center gap-2.5 px-4 py-2.5 text-xs text-white/50">
          <span class="size-1.5 flex-none rounded-full bg-fuchsia-500" />
          Automation moved NP-142 → Done when #318 merged
        </div>
      </div>
    </div>
  );
}

function AutomationsCard() {
  return (
    <div class="flex flex-col gap-5 rounded-3xl border border-[#e9e3f2] bg-white p-8">
      <span class="font-geist-mono text-xs text-violet-500">Automations</span>
      <h3 class="m-0 text-2xl leading-[1.15] font-[650] tracking-[-0.025em]">
        Set the rules. Skip the busywork.
      </h3>
      <div class="mt-auto flex flex-col text-sm">
        <div class="rounded-xl border border-[#e4d9f7] bg-[#f5f0fd] px-3.5 py-3">
          <span class="mr-2 font-geist-mono text-[11px] text-brand">WHEN</span>a pull request merges
        </div>
        <div class="ml-6 h-3.5 w-[1.5px] bg-[#d8ccef]" />
        <div class="rounded-xl border border-[#e9e3f2] bg-white px-3.5 py-3">
          <span class="mr-2 font-geist-mono text-[11px] text-brand">IF</span>task is in Review
        </div>
        <div class="ml-6 h-3.5 w-[1.5px] bg-[#d8ccef]" />
        <div class="rounded-xl bg-ink px-3.5 py-3 text-white">
          <span class="mr-2 font-geist-mono text-[11px] text-fuchsia-300">THEN</span>move to Done,
          notify assignee
        </div>
      </div>
    </div>
  );
}

function HistoryCard() {
  return (
    <div class="flex flex-col gap-5 rounded-3xl border border-[#e9e3f2] bg-white p-8">
      <span class="font-geist-mono text-xs text-violet-500">Activity history</span>
      <h3 class="m-0 text-2xl leading-[1.15] font-[650] tracking-[-0.025em]">
        See what changed, and who changed it.
      </h3>
      <div class="mt-auto flex flex-col gap-3.5">
        <For each={history}>
          {(entry) => (
            <div class="flex items-center gap-3 text-sm text-[#3d3552]">
              <span
                class="flex size-7 flex-none items-center justify-center rounded-full text-[11px] font-semibold text-white"
                style={{ background: entry.color }}
              >
                {entry.who}
              </span>
              <span class="min-w-0 flex-1 leading-[1.4]">{entry.text}</span>
              <span class="flex-none font-geist-mono text-[11px] text-[#9a92ad]">{entry.when}</span>
            </div>
          )}
        </For>
      </div>
    </div>
  );
}

function ReportsCard() {
  return (
    <div class="flex flex-col gap-5 rounded-3xl bg-linear-160 from-brand via-violet-500 via-55% to-fuchsia-600 p-8 text-white">
      <span class="font-geist-mono text-xs text-fuchsia-200">Reports</span>
      <h3 class="m-0 text-2xl leading-[1.15] font-[650] tracking-[-0.025em]">
        Know how delivery is going.
      </h3>
      <div class="mt-auto flex flex-wrap gap-2">
        <For each={reports}>
          {(report) => <span class="rounded-full bg-white/16 px-3.5 py-2 text-sm">{report}</span>}
        </For>
      </div>
      <span class="text-sm text-white/75">Counted in tasks, story points or hours.</span>
    </div>
  );
}

export default function CodeSection() {
  return (
    <section id="code" class="bg-mist px-6 py-30">
      <div class="mx-auto flex max-w-310 flex-col gap-12">
        <div class="flex max-w-190 flex-col gap-4.5">
          <span class="font-geist-mono text-xs tracking-[0.08em] text-brand uppercase">
            03 — Wired into the work
          </span>
          <h2 class="m-0 text-[clamp(38px,4.6vw,64px)] leading-none font-bold tracking-[-0.04em] text-balance">
            The task and the pull request, side by side.
          </h2>
        </div>

        <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          <GitHubCard />
          <AutomationsCard />
          <HistoryCard />
          <ReportsCard />
        </div>
      </div>
    </section>
  );
}
