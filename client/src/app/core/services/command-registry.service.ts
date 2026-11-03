import {
  assertInInjectionContext,
  computed,
  DestroyRef,
  inject,
  Service,
  signal,
  Signal,
} from '@angular/core';

export interface CommandPlace {
  path: string;
  fragment?: string;
  section: string;
}

export interface Command {
  id: string;
  label: string;
  keywords?: string[];
  group: 'navigation' | 'actions' | 'settings';
  icon?: string;
  shortcut?: readonly string[];
  place?: CommandPlace;
  execute: () => void;
}

export type CommandScope = 'view' | 'global';

interface SearchableCommand {
  command: Command;
  label: string;
  terms: readonly string[];
}

enum CommandMatch {
  exactLabel,
  labelWordStart,
  labelSubstring,
  term,
}

@Service()
export class CommandRegistry {
  private readonly viewSources = signal<readonly Signal<Command[]>[]>([]);
  private readonly globalSources = signal<readonly Signal<Command[]>[]>([]);

  private readonly commands = computed(() => {
    const sources = [...this.viewSources(), ...this.globalSources()];

    return sources.flatMap((source) => source());
  });

  private readonly searchable = computed(() => {
    return this.commands().map(toSearchableCommand);
  });

  private readonly browsable = computed(() => {
    return this.commands().filter((command) => !command.place);
  });

  add(source: Signal<Command[]>, scope: CommandScope) {
    const sources = scope === 'view' ? this.viewSources : this.globalSources;

    sources.update((current) => [...current, source]);

    return () => {
      sources.update((current) => current.filter((item) => item !== source));
    };
  }

  filter(q: string): Command[] {
    const query = foldForSearch(q.trim());

    if (!query) return this.browsable();

    const matches: { command: Command; match: CommandMatch }[] = [];

    for (const item of this.searchable()) {
      const match = matchCommand(item, query);

      if (match !== null) matches.push({ command: item.command, match });
    }

    return matches
      .sort((left, right) => left.match - right.match)
      .map((item) => item.command);
  }

  all(): Command[] {
    return this.commands();
  }
}

export function injectCommands(
  commands: () => Command[],
  scope: CommandScope = 'view'
) {
  assertInInjectionContext(injectCommands);

  const registry = inject(CommandRegistry);
  const remove = registry.add(computed(commands), scope);

  inject(DestroyRef).onDestroy(remove);
}

function toSearchableCommand(command: Command): SearchableCommand {
  const keywords = command.keywords ?? [];
  const section = command.place?.section;
  const sectionTerm = section ? [`${section} ${command.label}`] : [];

  return {
    command,
    label: foldForSearch(command.label),
    terms: [...keywords, ...sectionTerm].map(foldForSearch),
  };
}

function foldForSearch(text: string): string {
  return text.normalize('NFD').replace(/\p{M}/gu, '').toLowerCase();
}

function matchCommand(
  { label, terms }: SearchableCommand,
  query: string
): CommandMatch | null {
  if (label === query) return CommandMatch.exactLabel;

  const firstIndex = label.indexOf(query);

  if (firstIndex === -1) {
    const matchesTerm = terms.some((term) => term.includes(query));

    return matchesTerm ? CommandMatch.term : null;
  }

  const startsWord = hasWordStartMatch(label, query, firstIndex);

  return startsWord ? CommandMatch.labelWordStart : CommandMatch.labelSubstring;
}

function hasWordStartMatch(
  label: string,
  query: string,
  firstIndex: number
): boolean {
  let index = firstIndex;

  while (index !== -1) {
    const isWordStart = index === 0 || !isWordCharacter(label[index - 1]);

    if (isWordStart) return true;

    index = label.indexOf(query, index + 1);
  }

  return false;
}

function isWordCharacter(character: string): boolean {
  return /[\p{L}\p{N}]/u.test(character);
}
