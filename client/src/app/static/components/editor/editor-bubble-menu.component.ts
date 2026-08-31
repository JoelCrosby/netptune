import {
  Component,
  computed,
  effect,
  ElementRef,
  inject,
  input,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import {
  LucideBold,
  LucideCheck,
  LucideCode,
  LucideHeading1,
  LucideHeading2,
  LucideItalic,
  LucideLink,
  LucideStrikethrough,
  LucideUnlink,
  LucideX,
} from '@lucide/angular';
import type { Editor } from '@tiptap/core';
import {
  editorMenuButton,
  editorMenuIcon,
  editorMenuSeparator,
  editorMenuSurface,
} from './editor-menu.styles';

const inactive = {
  heading1: false,
  heading2: false,
  bold: false,
  italic: false,
  strike: false,
  code: false,
  link: false,
};

@Component({
  selector: 'app-editor-bubble-menu',
  imports: [
    LucideBold,
    LucideCheck,
    LucideCode,
    LucideHeading1,
    LucideHeading2,
    LucideItalic,
    LucideLink,
    LucideStrikethrough,
    LucideUnlink,
    LucideX,
  ],
  host: { class: 'absolute invisible z-50' },
  template: `
    @if (editor(); as editor) {
      <div [class]="surfaceClass" (mousedown)="$event.preventDefault()">
        @if (linkMode()) {
          <input
            #linkInput
            type="url"
            class="h-7 w-56 rounded bg-transparent px-2 text-sm outline-none"
            [attr.aria-label]="labels.linkUrl"
            [placeholder]="labels.linkUrl"
            [value]="linkHref()"
            (mousedown)="$event.stopPropagation()"
            (keydown.enter)="applyLink(editor, linkInput.value)"
            (keydown.escape)="linkMode.set(false)" />
          <button
            type="button"
            [class]="buttonClass"
            [attr.aria-label]="labels.applyLink"
            [title]="labels.applyLink"
            (click)="applyLink(editor, linkInput.value)">
            <svg lucideCheck [class]="iconClass"></svg>
          </button>
          <button
            type="button"
            [class]="buttonClass"
            [attr.aria-label]="labels.removeLink"
            [title]="labels.removeLink"
            (click)="removeLink(editor)">
            <svg lucideUnlink [class]="iconClass"></svg>
          </button>
          <button
            type="button"
            [class]="buttonClass"
            [attr.aria-label]="labels.cancel"
            [title]="labels.cancel"
            (click)="linkMode.set(false)">
            <svg lucideX [class]="iconClass"></svg>
          </button>
        } @else {
          <button
            type="button"
            [class]="buttonClass"
            [attr.data-active]="active().heading1"
            [attr.aria-pressed]="active().heading1"
            [attr.aria-label]="labels.heading1"
            [title]="labels.heading1"
            (click)="toggleHeading(editor, 1)">
            <svg lucideHeading1 [class]="iconClass"></svg>
          </button>
          <button
            type="button"
            [class]="buttonClass"
            [attr.data-active]="active().heading2"
            [attr.aria-pressed]="active().heading2"
            [attr.aria-label]="labels.heading2"
            [title]="labels.heading2"
            (click)="toggleHeading(editor, 2)">
            <svg lucideHeading2 [class]="iconClass"></svg>
          </button>

          <span [class]="separatorClass"></span>

          <button
            type="button"
            [class]="buttonClass"
            [attr.data-active]="active().bold"
            [attr.aria-pressed]="active().bold"
            [attr.aria-label]="labels.bold"
            [title]="labels.bold"
            (click)="run(editor, 'toggleBold')">
            <svg lucideBold [class]="iconClass"></svg>
          </button>
          <button
            type="button"
            [class]="buttonClass"
            [attr.data-active]="active().italic"
            [attr.aria-pressed]="active().italic"
            [attr.aria-label]="labels.italic"
            [title]="labels.italic"
            (click)="run(editor, 'toggleItalic')">
            <svg lucideItalic [class]="iconClass"></svg>
          </button>
          <button
            type="button"
            [class]="buttonClass"
            [attr.data-active]="active().strike"
            [attr.aria-pressed]="active().strike"
            [attr.aria-label]="labels.strike"
            [title]="labels.strike"
            (click)="run(editor, 'toggleStrike')">
            <svg lucideStrikethrough [class]="iconClass"></svg>
          </button>
          <button
            type="button"
            [class]="buttonClass"
            [attr.data-active]="active().code"
            [attr.aria-pressed]="active().code"
            [attr.aria-label]="labels.code"
            [title]="labels.code"
            (click)="run(editor, 'toggleCode')">
            <svg lucideCode [class]="iconClass"></svg>
          </button>

          <span [class]="separatorClass"></span>

          <button
            type="button"
            [class]="buttonClass"
            [attr.data-active]="active().link"
            [attr.aria-pressed]="active().link"
            [attr.aria-label]="labels.link"
            [title]="labels.link"
            (click)="openLink(editor)">
            <svg lucideLink [class]="iconClass"></svg>
          </button>
        }
      </div>
    }
  `,
})
export class EditorBubbleMenuComponent {
  readonly el = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly editor = input<Editor | null>(null);
  readonly revision = input(0);

  protected readonly surfaceClass = editorMenuSurface;
  protected readonly buttonClass = editorMenuButton;
  protected readonly separatorClass = editorMenuSeparator;
  protected readonly iconClass = editorMenuIcon;

  protected readonly linkMode = signal(false);
  protected readonly linkHref = signal('');

  private readonly linkInput =
    viewChild<ElementRef<HTMLInputElement>>('linkInput');

  protected readonly labels = {
    heading1: $localize`:Accessible label for the editor's heading 1 button:Heading 1`,
    heading2: $localize`:Accessible label for the editor's heading 2 button:Heading 2`,
    bold: $localize`:Accessible label for the editor's bold button:Bold`,
    italic: $localize`:Accessible label for the editor's italic button:Italic`,
    strike: $localize`:Accessible label for the editor's strikethrough button:Strikethrough`,
    code: $localize`:Accessible label for the editor's inline code button:Inline Code`,
    link: $localize`:Accessible label for the editor's link button:Link`,
    linkUrl: $localize`:Label of the box holding the address of an editor link:Link Address`,
    applyLink: $localize`:Accessible label for the button that applies an editor link:Apply Link`,
    removeLink: $localize`:Accessible label for the button that removes an editor link:Remove Link`,
    cancel: $localize`:Accessible label for the button that closes the editor link box:Cancel`,
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
      bold: editor.isActive('bold'),
      italic: editor.isActive('italic'),
      strike: editor.isActive('strike'),
      code: editor.isActive('code'),
      link: editor.isActive('link'),
    };
  });

  constructor() {
    effect(() => {
      this.revision();

      untracked(() => this.linkMode.set(false));
    });

    effect(() => {
      if (!this.linkMode()) return;

      this.linkInput()?.nativeElement.focus();
    });
  }

  protected run(editor: Editor, command: BubbleCommand) {
    editor.chain().focus()[command]().run();
  }

  protected toggleHeading(editor: Editor, level: 1 | 2) {
    editor.chain().focus().toggleHeading({ level }).run();
  }

  protected openLink(editor: Editor) {
    this.linkHref.set(String(editor.getAttributes('link')['href'] ?? ''));
    this.linkMode.set(true);
  }

  protected applyLink(editor: Editor, href: string) {
    const trimmed = href.trim();

    if (!trimmed) {
      this.removeLink(editor);
      return;
    }

    editor
      .chain()
      .focus()
      .extendMarkRange('link')
      .setLink({ href: trimmed })
      .run();

    this.linkMode.set(false);
  }

  protected removeLink(editor: Editor) {
    editor.chain().focus().extendMarkRange('link').unsetLink().run();
    this.linkMode.set(false);
  }
}

type BubbleCommand =
  'toggleBold' | 'toggleItalic' | 'toggleStrike' | 'toggleCode';
