import type { Editor } from '@tiptap/core';
import {
  LucideAtSign,
  LucideListTodo,
  LucidePenLine,
  LucideTextQuote,
} from '@lucide/angular';
import { EditorCommand } from '@static/components/editor/editor-commands';
import { trackRange } from './editor-range-tracker';
import { queryEmbedNodeType } from './query-embed-syntax';

export interface PageCommandHandlers {
  canEmbedQueries: boolean;
  canUseAssistant: boolean;
  pickQuery(): Promise<string | undefined>;
  // Works on the page above the position and offers the result to insert there.
  transformAbove(action: 'continue' | 'summarise', position: number): void;
}

const netptuneGroup = $localize`:Heading over the Netptune blocks in the slash command menu:Netptune`;
const assistantGroup = $localize`:Heading over the assistant actions in the slash command menu:Assistant`;

export function createPageEditorCommands(
  handlers: PageCommandHandlers
): EditorCommand[] {
  const mention: EditorCommand = {
    id: 'mention',
    label: $localize`:Slash command that starts mentioning a task or page:Mention a task or page`,
    group: netptuneGroup,
    icon: LucideAtSign,
    hint: '@',
    keywords: ['link', 'task', 'page', 'reference'],
    run: (editor, range) => {
      editor.chain().focus().deleteRange(range).insertContent('@').run();
    },
  };

  const liveTaskList: EditorCommand = {
    id: 'live-task-list',
    label: $localize`:Slash command that embeds a live list of a saved query's tasks:Live task list`,
    group: netptuneGroup,
    icon: LucideListTodo,
    keywords: ['query', 'embed', 'tasks', 'saved'],
    run: (editor, range) => {
      editor.chain().focus().deleteRange(range).run();
      void insertQueryEmbed(editor, range.from, handlers);
    },
  };

  const continueWriting: EditorCommand = {
    id: 'continue-writing',
    label: $localize`:Slash command that has the assistant write the next part of a page:Continue writing`,
    group: assistantGroup,
    icon: LucidePenLine,
    keywords: ['ai', 'assistant', 'write', 'draft'],
    run: (editor, range) => {
      editor.chain().focus().deleteRange(range).run();
      handlers.transformAbove('continue', range.from);
    },
  };

  const summariseAbove: EditorCommand = {
    id: 'summarise-above',
    label: $localize`:Slash command that has the assistant summarise the page above the cursor:Summarise above`,
    group: assistantGroup,
    icon: LucideTextQuote,
    keywords: ['ai', 'assistant', 'summary', 'tldr'],
    run: (editor, range) => {
      editor.chain().focus().deleteRange(range).run();
      handlers.transformAbove('summarise', range.from);
    },
  };

  const netptune = handlers.canEmbedQueries
    ? [mention, liveTaskList]
    : [mention];
  const assistant = handlers.canUseAssistant
    ? [continueWriting, summariseAbove]
    : [];

  return [...netptune, ...assistant];
}

async function insertQueryEmbed(
  editor: Editor,
  from: number,
  handlers: PageCommandHandlers
) {
  const tracker = trackRange(editor, { from, to: from });
  const slug = await handlers.pickQuery();
  const range = tracker.current();

  tracker.stop();

  // The document was replaced while the picker was open, so there is no place left to put the embed.
  if (editor.isDestroyed || !range) return;

  const position = range.from;

  if (!slug) {
    editor.commands.focus(position);

    return;
  }

  editor
    .chain()
    .focus()
    .insertContentAt(position, [
      { type: queryEmbedNodeType, attrs: { slug } },
      { type: 'paragraph' },
    ])
    .run();
}
