import DocSearch from './DocSearch';

export default function DocsNav() {
  return (
    <header class="sticky top-0 z-50 border-b border-purple-200/8 bg-[rgb(8_5_16/0.86)] backdrop-blur-[14px]">
      <div class="mx-auto flex max-w-330 flex-wrap items-center justify-between gap-x-6 px-6 md:h-16 md:flex-nowrap">
        <div class="flex h-16 shrink-0 items-center gap-3.5">
          <a href="/" class="flex items-center gap-2.5 text-white hover:text-white">
            <img src="/favicon.svg" alt="Netptune logo" class="size-7.5 rounded-lg" />
            <span class="text-lg font-semibold tracking-[-0.02em]">Netptune</span>
          </a>
          <a
            href="/docs"
            class="rounded-md border border-purple-200/20 px-2 py-0.75 font-geist-mono text-[11px] text-purple-200 transition-colors hover:border-purple-200/40"
          >
            DOCS
          </a>
        </div>

        <div class="order-last w-full pb-3 md:order-none md:w-auto md:max-w-105 md:min-w-0 md:flex-1 md:pb-0">
          <DocSearch />
        </div>

        <div class="flex shrink-0 items-center gap-2">
          <a
            href="/"
            class="hidden px-3 py-2 text-sm whitespace-nowrap text-white/70 transition-colors hover:text-white sm:block"
          >
            Home
          </a>
          <a
            href="https://app.netptune.co.uk/auth/login"
            class="rounded-full bg-brand px-4 py-2.25 text-sm font-medium whitespace-nowrap text-white shadow-[inset_0_0_0_1px_rgb(233_213_255/0.25)] transition-colors hover:bg-[#7c4dd0]"
          >
            Open Netptune
          </a>
        </div>
      </div>
    </header>
  );
}
