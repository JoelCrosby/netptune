export default function CtaSection() {
  return (
    <section class="relative overflow-hidden bg-night px-6 pt-35 pb-65 text-center font-geist text-white">
      <div class="cta-stars" />
      <div class="cta-horizon" />
      <div class="relative mx-auto flex max-w-190 flex-col items-center gap-6">
        <h2 class="m-0 font-geist text-[clamp(48px,6.4vw,96px)] leading-[0.95] font-bold tracking-[-0.05em]">
          Ready when your team is.
        </h2>
        <p class="m-0 max-w-130 text-[19px] leading-[1.55] text-white/62">
          Create a workspace in a minute and invite your team. Add an Anthropic or OpenAI key
          whenever you'd like the assistant to take care of the busywork.
        </p>
        <div class="flex flex-wrap justify-center gap-3 pt-2">
          <a
            href="https://app.netptune.co.uk/auth/register"
            class="rounded-full bg-white px-6.5 py-3.75 text-base font-semibold text-ink transition-colors hover:bg-purple-100"
          >
            Create an account →
          </a>
          <a
            href="https://app.netptune.co.uk/auth/login"
            class="rounded-full border border-purple-200/28 px-6.5 py-3.75 text-base font-medium text-white transition-colors hover:bg-purple-200/8"
          >
            Sign in
          </a>
        </div>
      </div>
    </section>
  );
}
