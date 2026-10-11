import type { ChainedCommands, Editor, Range } from '@tiptap/core';
import {
  LucideCode,
  LucideHeading2,
  LucideHeading3,
  LucideInfo,
  LucideList,
  LucideListChecks,
  LucideListOrdered,
  LucideMinus,
  LucideQuote,
  type LucideIconInput,
} from '@lucide/angular';

// Something the / menu offers. `run` receives the editor and the range of the typed /query, which it
// is expected to replace.
export interface EditorCommand {
  id: string;
  label: string;
  group: string;
  icon: LucideIconInput;
  hint?: string;
  keywords?: readonly string[];
  run: (editor: Editor, range: Range) => void;
}

const basicGroup = $localize`:Heading over the basic blocks in the slash command menu:Basic`;

export const basicEditorCommands: readonly EditorCommand[] = [
  {
    id: 'heading-2',
    label: $localize`:Slash command that inserts a heading:Heading`,
    group: basicGroup,
    icon: LucideHeading2,
    hint: '##',
    keywords: ['title', 'h2'],
    run: replaceRangeWith((chain) => chain.setHeading({ level: 2 })),
  },
  {
    id: 'heading-3',
    label: $localize`:Slash command that inserts a smaller heading:Subheading`,
    group: basicGroup,
    icon: LucideHeading3,
    hint: '###',
    keywords: ['h3'],
    run: replaceRangeWith((chain) => chain.setHeading({ level: 3 })),
  },
  {
    id: 'checklist',
    label: $localize`:Slash command that inserts a checklist:Checklist`,
    group: basicGroup,
    icon: LucideListChecks,
    hint: '[ ]',
    keywords: ['todo', 'task'],
    run: replaceRangeWith((chain) => chain.toggleTaskList()),
  },
  {
    id: 'bullet-list',
    label: $localize`:Slash command that inserts a bulleted list:Bulleted list`,
    group: basicGroup,
    icon: LucideList,
    hint: '-',
    run: replaceRangeWith((chain) => chain.toggleBulletList()),
  },
  {
    id: 'numbered-list',
    label: $localize`:Slash command that inserts a numbered list:Numbered list`,
    group: basicGroup,
    icon: LucideListOrdered,
    hint: '1.',
    run: replaceRangeWith((chain) => chain.toggleOrderedList()),
  },
  {
    id: 'code-block',
    label: $localize`:Slash command that inserts a code block:Code block`,
    group: basicGroup,
    icon: LucideCode,
    hint: '```',
    run: replaceRangeWith((chain) => chain.setCodeBlock()),
  },
  {
    id: 'callout',
    label: $localize`:Slash command that inserts a highlighted callout block:Callout`,
    group: basicGroup,
    icon: LucideInfo,
    keywords: ['note', 'info', 'warning', 'alert'],
    run: replaceRangeWith((chain) => chain.wrapIn('callout')),
  },
  {
    id: 'quote',
    label: $localize`:Slash command that inserts a quote:Quote`,
    group: basicGroup,
    icon: LucideQuote,
    hint: '>',
    run: replaceRangeWith((chain) => chain.setBlockquote()),
  },
  {
    id: 'divider',
    label: $localize`:Slash command that inserts a horizontal divider:Divider`,
    group: basicGroup,
    icon: LucideMinus,
    hint: '---',
    keywords: ['rule', 'separator', 'hr'],
    run: replaceRangeWith((chain) => chain.setHorizontalRule()),
  },
];

// Checked first, so a block that cannot go here leaves the typed /query in place instead of
// deleting it and doing nothing else.
function replaceRangeWith(
  apply: (chain: ChainedCommands) => ChainedCommands
): EditorCommand['run'] {
  return (editor, range) => {
    const applies = apply(editor.can().chain().deleteRange(range)).run();

    if (!applies) return;

    apply(editor.chain().focus().deleteRange(range)).run();
  };
}

export function matchesCommand(command: EditorCommand, query: string): boolean {
  const term = query.trim().toLocaleLowerCase();

  if (!term) return true;

  const words = [command.label, ...(command.keywords ?? [])];

  return words.some((word) => word.toLocaleLowerCase().includes(term));
}
