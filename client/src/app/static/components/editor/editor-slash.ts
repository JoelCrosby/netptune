import { computed, signal } from '@angular/core';
import { Extension, type Editor, type Range } from '@tiptap/core';
import { PluginKey } from '@tiptap/pm/state';
import {
  Suggestion,
  type SuggestionKeyDownProps,
  type SuggestionProps,
} from '@tiptap/suggestion';
import { SuggestionItem } from '../suggestion-menu/suggestion-menu.component';
import { EditorCommand, matchesCommand } from './editor-commands';
import {
  closeSuggestionOnBlur,
  initialSuggestionMenuPosition,
  isOutsideCode,
  suggestionMenuPosition,
} from './editor-suggestion';

const slashSuggestionKey = new PluginKey('slashCommandSuggestion');

type SlashProps = SuggestionProps<unknown, EditorCommand>;

export class SlashMenu {
  readonly open = signal(false);
  readonly activeIndex = signal(0);
  readonly position = signal(initialSuggestionMenuPosition);

  private readonly query = signal('');
  private readonly available = signal<readonly EditorCommand[]>([]);
  private editor: Editor | null = null;
  private range: Range | null = null;

  readonly matches = computed(() => {
    const query = this.query();

    return this.available().filter((command) => matchesCommand(command, query));
  });

  readonly items = computed<SuggestionItem[]>(() => {
    return this.matches().map((command) => ({
      id: command.id,
      label: command.label,
      hint: command.hint ?? null,
      icon: command.icon,
      group: command.group,
    }));
  });

  setCommands(commands: readonly EditorCommand[]) {
    this.available.set(commands);
  }

  pick(index: number) {
    const command = this.matches().at(index);
    const editor = this.editor;
    const range = this.range;

    if (!command || !editor || !range) return;

    this.close();
    command.run(editor, range);
  }

  start(props: SlashProps) {
    this.open.set(true);
    this.update(props);
  }

  update(props: SlashProps) {
    this.editor = props.editor;
    this.range = props.range;
    this.query.set(props.query);
    this.activeIndex.set(0);

    const rect = props.clientRect?.();

    if (rect) {
      this.position.set(suggestionMenuPosition(rect));
    }
  }

  keyDown({ event }: SuggestionKeyDownProps): boolean {
    const count = this.matches().length;

    switch (event.key) {
      case 'ArrowDown':
        if (!count) return false;

        this.move(1, count);

        return true;
      case 'ArrowUp':
        if (!count) return false;

        this.move(-1, count);

        return true;
      case 'Enter':
      case 'Tab':
        if (!count) return false;

        this.pick(this.activeIndex());

        return true;
      case 'Escape':
        this.close();

        return true;
      default:
        return false;
    }
  }

  close() {
    this.open.set(false);
  }

  private move(step: number, count: number) {
    this.activeIndex.update((index) => (index + step + count) % count);
  }
}

export function createSlashExtension(menu: SlashMenu) {
  return Extension.create({
    name: 'slashCommand',

    addProseMirrorPlugins() {
      return [
        Suggestion<unknown, EditorCommand>({
          pluginKey: slashSuggestionKey,
          editor: this.editor,
          char: '/',
          allow: isOutsideCode,
          items: () => [],
          render: () => ({
            onStart: (props) => menu.start(props),
            onUpdate: (props) => menu.update(props),
            onKeyDown: (props) => menu.keyDown(props),
            onExit: () => menu.close(),
          }),
        }),
        closeSuggestionOnBlur(slashSuggestionKey),
      ];
    },
  });
}
