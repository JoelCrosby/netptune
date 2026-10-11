import type { Editor, EditorEvents } from '@tiptap/core';
import { LucideAtSign, LucideListTodo } from '@lucide/angular';
import { EditorCommand } from '@static/components/editor/editor-commands';
import { queryEmbedNodeType } from './query-embed-syntax';

export interface PageCommandHandlers {
  canEmbedQueries: boolean;
  pickQuery(): Promise<string | undefined>;
}

const netptuneGroup = $localize`:Heading over the Netptune blocks in the slash command menu:Netptune`;

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

  return handlers.canEmbedQueries ? [mention, liveTaskList] : [mention];
}

async function insertQueryEmbed(
  editor: Editor,
  from: number,
  handlers: PageCommandHandlers
) {
  const anchor = trackPosition(editor, from);
  const slug = await handlers.pickQuery();
  const position = anchor.stop();

  if (editor.isDestroyed) return;

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

// Follows a document position through edits made while the picker is open, such as a save
// replacing the document, so the embed lands where /live was typed.
function trackPosition(editor: Editor, start: number) {
  let position = start;

  const follow = ({ transaction }: EditorEvents['transaction']) => {
    position = transaction.mapping.map(position);
  };

  editor.on('transaction', follow);

  return {
    stop: () => {
      editor.off('transaction', follow);

      return Math.min(position, editor.state.doc.content.size);
    },
  };
}
