import { For, createSignal } from 'solid-js';
import { assistantBandItems, assistantProposals } from '~/data/assistant-band';

function ProposalCard() {
  const [accepted, setAccepted] = createSignal(
    assistantProposals.map((proposal) => proposal.defaultAccepted),
  );

  const acceptedCount = () => accepted().filter(Boolean).length;

  const applyLabel = () => {
    const count = acceptedCount();

    if (count === 0) {
      return 'Nothing selected';
    }

    return `Apply ${count} change${count > 1 ? 's' : ''}`;
  };

  function toggle(index: number) {
    setAccepted((current) => current.map((value, i) => (i === index ? !value : value)));
  }

  return (
    <div class="relative">
      <div class="absolute inset-[8%_-4%_-6%_6%] rounded-3xl bg-linear-140 from-brand to-fuchsia-500 opacity-35 blur-[60px]" />
      <div class="relative overflow-hidden rounded-[20px] border border-purple-200/14 bg-[rgb(22_15_40/0.92)] shadow-[0_30px_80px_rgb(0_0_0/0.5)]">
        <div class="flex items-center gap-2.5 border-b border-purple-200/10 px-5 py-4">
          <span class="flex size-6.5 items-center justify-center rounded-lg bg-linear-135 from-violet-500 to-fuchsia-500 text-[13px] font-bold">
            ✦
          </span>
          <span class="text-sm font-semibold">Assistant</span>
          <span class="ml-auto font-geist-mono text-[11px] text-white/45">Sprint 14 · Web app</span>
        </div>

        <div class="flex flex-col gap-4 p-5">
          <div class="max-w-[80%] self-end rounded-[14px_14px_4px_14px] bg-brand px-3.75 py-2.75 text-sm leading-normal">
            What slipped last sprint? Move anything still open into this one.
          </div>
          <div class="text-sm leading-[1.6] text-white/78">
            Three tasks didn't make it. I've drafted the moves — pick what goes in.
          </div>

          <div class="overflow-hidden rounded-[14px] border border-purple-200/14 bg-white/3">
            <For each={assistantProposals}>
              {(proposal, i) => {
                const isAccepted = () => accepted()[i()];

                return (
                  <button
                    type="button"
                    role="checkbox"
                    aria-checked={isAccepted()}
                    class="flex w-full cursor-pointer items-center gap-3.5 border-b border-purple-200/8 px-4 py-3.25 text-left transition-colors hover:bg-purple-200/5"
                    onClick={() => toggle(i())}
                  >
                    <span
                      class="flex size-5 flex-none items-center justify-center rounded-md border-[1.5px] text-xs font-bold"
                      classList={{
                        'border-violet-500 bg-violet-500': isAccepted(),
                        'border-purple-200/35': !isAccepted(),
                      }}
                    >
                      {isAccepted() ? '✓' : ''}
                    </span>
                    <span class="flex-none font-geist-mono text-xs text-violet-300">
                      {proposal.id}
                    </span>
                    <span
                      class="min-w-0 flex-1 text-sm"
                      classList={{
                        'text-white': isAccepted(),
                        'text-white/50': !isAccepted(),
                      }}
                    >
                      {proposal.text}
                    </span>
                    <span class="hidden flex-none rounded-md bg-purple-200/8 px-2 py-0.75 font-geist-mono text-[11px] text-white/60 sm:inline">
                      {proposal.change}
                    </span>
                  </button>
                );
              }}
            </For>
            <div class="flex items-center justify-between gap-3 bg-purple-200/4 px-4 py-3">
              <span class="text-xs text-white/50">Nothing changes until you apply</span>
              <span class="rounded-full bg-white px-3.5 py-2 text-[13px] font-semibold text-ink">
                {applyLabel()}
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default function AssistantBand() {
  return (
    <section
      id="assistant"
      class="relative overflow-hidden bg-[#0d0819] bg-[image:radial-gradient(ellipse_50%_60%_at_85%_20%,rgb(217_70_239/0.22),transparent_70%),radial-gradient(ellipse_60%_70%_at_10%_90%,rgb(103_58_183/0.45),transparent_70%)] px-6 py-30 text-white"
    >
      <div class="relative mx-auto flex max-w-310 flex-col gap-18">
        <div class="grid items-center gap-14 lg:grid-cols-2">
          <div class="flex flex-col gap-5.5">
            <span class="font-geist-mono text-xs tracking-[0.08em] text-fuchsia-300 uppercase">
              02 — The assistant
            </span>
            <h2 class="m-0 text-[clamp(40px,5vw,72px)] leading-[0.98] font-bold tracking-[-0.045em] text-balance">
              Help when you want it. Never behind your back.
            </h2>
            <p class="m-0 max-w-125 text-lg leading-[1.6] text-pretty text-white/62">
              It suggests and you decide. It sees only what you can see, and nothing changes until
              you've had a look — then you can still undo it.
            </p>
            <a
              href="/assistant"
              class="inline-flex items-center gap-2.5 self-start rounded-full bg-white px-5.5 py-3.25 text-[15px] font-semibold text-ink transition-colors hover:bg-purple-100"
            >
              See what it can do →
            </a>
          </div>

          <ProposalCard />
        </div>

        <div class="grid border-t border-purple-200/14 sm:grid-cols-2 lg:grid-cols-3">
          <For each={assistantBandItems}>
            {(item, i) => (
              <a
                href={item.href}
                class="flex flex-col gap-2.5 border-b border-purple-200/10 pt-7 pr-7 pb-8 text-white transition-colors hover:text-fuchsia-300"
              >
                <span class="font-geist-mono text-xs text-fuchsia-300/80">
                  {String(i() + 1).padStart(2, '0')}
                </span>
                <span class="text-[19px] font-semibold tracking-[-0.02em]">{item.title}</span>
                <span class="text-[15px] leading-[1.55] text-pretty text-white/58">
                  {item.description}
                </span>
              </a>
            )}
          </For>
        </div>
      </div>
    </section>
  );
}
