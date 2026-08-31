import {
  Component,
  computed,
  ElementRef,
  inject,
  input,
  output,
} from '@angular/core';
import {
  LucideHeading1,
  LucideHeading2,
  LucideImage,
  LucideListOrdered,
  LucideListTodo,
  LucideList,
  LucideMinus,
  LucideQuote,
  LucideSquareCode,
} from '@lucide/angular';
import type { Editor } from '@tiptap/core';
import {
  editorMenuButton,
  editorMenuIcon,
  editorMenuSeparator,
  editorMenuSurface,
} from './editor-menu.styles';

@Component({
  selector: 'app-editor-floating-menu',
  imports: [
    LucideHeading1,
    LucideHeading2,
    LucideImage,
    LucideList,
    LucideListOrdered,
    LucideListTodo,
    LucideMinus,
    LucideQuote,
    LucideSquareCode,
  ],
  host: { class: 'absolute invisible z-50' },
  template: `
    @if (editor(); as editor) {
      <div [class]="surfaceClass" (mousedown)="$event.preventDefault()">
        <button
          type="button"
          [class]="buttonClass"
          [attr.data-active]="active().heading1"
          [attr.aria-label]="labels.heading1"
          [title]="labels.heading1"
          (click)="toggleHeading(editor, 1)">
          <svg lucideHeading1 [class]="iconClass"></svg>
        </button>
        <button
          type="button"
          [class]="buttonClass"
          [attr.data-active]="active().heading2"
          [attr.aria-label]="labels.heading2"
          [title]="labels.heading2"
          (click)="toggleHeading(editor, 2)">
          <svg lucideHeading2 [class]="iconClass"></svg>
        </button>

        <span [class]="separatorClass"></span>

        <button
          type="button"
          [class]="buttonClass"
          [attr.data-active]="active().bulletList"
          [attr.aria-label]="labels.bulletList"
          [title]="labels.bulletList"
          (click)="run(editor, 'toggleBulletList')">
          <svg lucideList [class]="iconClass"></svg>
        </button>
        <button
          type="button"
          [class]="buttonClass"
          [attr.data-active]="active().orderedList"
          [attr.aria-label]="labels.orderedList"
          [title]="labels.orderedList"
          (click)="run(editor, 'toggleOrderedList')">
          <svg lucideListOrdered [class]="iconClass"></svg>
        </button>
        <button
          type="button"
          [class]="buttonClass"
          [attr.data-active]="active().taskList"
          [attr.aria-label]="labels.taskList"
          [title]="labels.taskList"
          (click)="run(editor, 'toggleTaskList')">
          <svg lucideListTodo [class]="iconClass"></svg>
        </button>

        <span [class]="separatorClass"></span>

        <button
          type="button"
          [class]="buttonClass"
          [attr.data-active]="active().blockquote"
          [attr.aria-label]="labels.quote"
          [title]="labels.quote"
          (click)="run(editor, 'toggleBlockquote')">
          <svg lucideQuote [class]="iconClass"></svg>
        </button>
        <button
          type="button"
          [class]="buttonClass"
          [attr.data-active]="active().codeBlock"
          [attr.aria-label]="labels.codeBlock"
          [title]="labels.codeBlock"
          (click)="run(editor, 'toggleCodeBlock')">
          <svg lucideSquareCode [class]="iconClass"></svg>
        </button>
        <button
          type="button"
          [class]="buttonClass"
          [attr.aria-label]="labels.divider"
          [title]="labels.divider"
          (click)="insertDivider(editor)">
          <svg lucideMinus [class]="iconClass"></svg>
        </button>
        <button
          type="button"
          [class]="buttonClass"
          [attr.aria-label]="labels.file"
          [title]="labels.file"
          (click)="fileRequested.emit()">
          <svg lucideImage [class]="iconClass"></svg>
        </button>
      </div>
    }
  `,
})
export class EditorFloatingMenuComponent {
  readonly el = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly editor = input<Editor | null>(null);
  readonly revision = input(0);

  readonly fileRequested = output();

  protected readonly surfaceClass = editorMenuSurface;
  protected readonly buttonClass = editorMenuButton;
  protected readonly separatorClass = editorMenuSeparator;
  protected readonly iconClass = editorMenuIcon;

  protected readonly labels = {
    heading1: $localize`:Accessible label for the editor's heading 1 button:Heading 1`,
    heading2: $localize`:Accessible label for the editor's heading 2 button:Heading 2`,
    bulletList: $localize`:Accessible label for the editor's bulleted list button:Bulleted List`,
    orderedList: $localize`:Accessible label for the editor's numbered list button:Numbered List`,
    taskList: $localize`:Accessible label for the editor's checklist button:Checklist`,
    quote: $localize`:Accessible label for the editor's quote button:Quote`,
    codeBlock: $localize`:Accessible label for the editor's code block button:Code Block`,
    divider: $localize`:Accessible label for the editor's divider button:Divider`,
    file: $localize`:Accessible label for the button that adds a file to the editor:Add File`,
  };

  protected readonly active = computed(() => {
    const editor = this.editor();

    // every button state is derived from the editor rather than a signal, so the
    // revision is read to pull them forward on each transaction
    this.revision();

    if (!editor) return inactive;

    return {
      heading1: editor.isActive('heading', { level: 1 }),
      heading2: editor.isActive('heading', { level: 2 }),
      bulletList: editor.isActive('bulletList'),
      orderedList: editor.isActive('orderedList'),
      taskList: editor.isActive('taskList'),
      blockquote: editor.isActive('blockquote'),
      codeBlock: editor.isActive('codeBlock'),
    };
  });

  protected run(editor: Editor, command: FloatingCommand) {
    editor.chain().focus()[command]().run();
  }

  protected toggleHeading(editor: Editor, level: 1 | 2) {
    editor.chain().focus().toggleHeading({ level }).run();
  }

  protected insertDivider(editor: Editor) {
    editor.chain().focus().setHorizontalRule().run();
  }
}

const inactive = {
  heading1: false,
  heading2: false,
  bulletList: false,
  orderedList: false,
  taskList: false,
  blockquote: false,
  codeBlock: false,
};

type FloatingCommand =
  | 'toggleBulletList'
  | 'toggleOrderedList'
  | 'toggleTaskList'
  | 'toggleBlockquote'
  | 'toggleCodeBlock';
