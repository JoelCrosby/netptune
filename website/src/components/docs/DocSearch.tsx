import { For, Show, createMemo, createSignal, onCleanup, onMount } from 'solid-js';
import { useNavigate } from '@solidjs/router';
import { Search } from 'lucide-solid';
import type { DocSearchResult } from '~/lib/docs-search';

function isTypingTarget(target: EventTarget | null): boolean {
  return (
    target instanceof HTMLElement &&
    (target.isContentEditable || ['INPUT', 'TEXTAREA', 'SELECT'].includes(target.tagName))
  );
}

export default function DocSearch() {
  const navigate = useNavigate();
  const [query, setQuery] = createSignal('');
  const [open, setOpen] = createSignal(false);
  const [highlighted, setHighlighted] = createSignal(0);
  const [searchDocs, setSearchDocs] = createSignal<(query: string) => DocSearchResult[]>();
  const results = createMemo(() => searchDocs()?.(query()) ?? []);
  const showResults = () => Boolean(open() && query().trim() && searchDocs());
  let input: HTMLInputElement | undefined;
  let indexRequested = false;

  function loadIndex() {
    if (indexRequested) return;

    indexRequested = true;
    void import('~/lib/docs-search').then((module) => setSearchDocs(() => module.searchDocs));
  }

  onMount(() => {
    const focusOnSlash = (event: KeyboardEvent) => {
      if (event.key !== '/') return;

      const isTyping = isTypingTarget(event.target);

      if (isTyping) return;

      event.preventDefault();
      input?.focus();
    };

    window.addEventListener('keydown', focusOnSlash);
    onCleanup(() => window.removeEventListener('keydown', focusOnSlash));
  });

  const go = (href: string) => {
    setOpen(false);
    setQuery('');
    input?.blur();
    navigate(href);
  };

  const onKeyDown = (event: KeyboardEvent) => {
    const count = results().length;

    if (event.key === 'ArrowDown' && count > 0) {
      event.preventDefault();
      setHighlighted((highlighted() + 1) % count);
    } else if (event.key === 'ArrowUp' && count > 0) {
      event.preventDefault();
      setHighlighted((highlighted() - 1 + count) % count);
    } else if (event.key === 'Enter' && count > 0) {
      event.preventDefault();
      go(results()[highlighted()].href);
    } else if (event.key === 'Escape') {
      setOpen(false);
      input?.blur();
    }
  };

  return (
    <div class="relative w-full">
      <label class="flex items-center gap-2.5 rounded-full border border-purple-200/14 bg-white/6 px-3 py-2 text-sm text-white/45 transition-colors focus-within:border-purple-200/35 focus-within:bg-white/10">
        <Search size={15} class="shrink-0" />
        <input
          ref={input}
          type="search"
          role="combobox"
          value={query()}
          placeholder="Search the docs"
          aria-label="Search the docs"
          aria-autocomplete="list"
          aria-controls="doc-search-results"
          aria-expanded={showResults()}
          aria-activedescendant={
            showResults() && results().length > 0 ? `doc-search-result-${highlighted()}` : undefined
          }
          class="min-w-0 flex-1 bg-transparent text-white outline-none placeholder:text-white/45 [&::-webkit-search-cancel-button]:hidden"
          onInput={(event) => {
            setQuery(event.currentTarget.value);
            setHighlighted(0);
            setOpen(true);
          }}
          onFocus={() => {
            setOpen(true);
            loadIndex();
          }}
          onBlur={() => setOpen(false)}
          onKeyDown={onKeyDown}
        />
        <kbd class="rounded-[5px] bg-white/8 px-1.5 py-0.5 font-geist-mono text-[11px] text-white/60">
          /
        </kbd>
      </label>

      <Show when={showResults()}>
        <div class="absolute inset-x-0 top-full mt-2 overflow-hidden rounded-2xl border border-hairline bg-white py-1.5 shadow-[0_24px_60px_-20px_rgb(20_15_34/0.45)]">
          <Show
            when={results().length > 0}
            fallback={
              <p role="status" class="m-0 px-4 py-3 text-sm text-ink-subtle">
                No matches
              </p>
            }
          >
            <div id="doc-search-results" role="listbox" aria-label="Search results">
              <For each={results()}>
                {(result, index) => (
                  <a
                    href={result.href}
                    id={`doc-search-result-${index()}`}
                    role="option"
                    aria-selected={index() === highlighted()}
                    tabIndex={-1}
                    class="flex flex-col gap-0.5 px-4 py-2.5"
                    classList={{ 'bg-[#f5f0fd]': index() === highlighted() }}
                    onMouseDown={(event) => event.preventDefault()}
                    onMouseEnter={() => setHighlighted(index())}
                    onClick={(event) => {
                      event.preventDefault();
                      go(result.href);
                    }}
                  >
                    <span class="text-sm font-medium text-ink">{result.title}</span>
                    <span class="font-geist-mono text-[11px] text-ink-subtle">
                      {result.context}
                    </span>
                  </a>
                )}
              </For>
            </div>
          </Show>
        </div>
      </Show>
    </div>
  );
}
