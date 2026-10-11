import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import {
  ApplicationRef,
  Component,
  DestroyRef,
  createComponent,
  EnvironmentInjector,
  Injector,
  computed,
  effect,
  inject,
  input,
  OnInit,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { Router } from '@angular/router';
import {
  PageKind,
  PageLinkedTask,
  PageLinks,
  PageTag,
  PageTransformAction,
} from '@core/models/pages';
import { fromNow } from '@core/util/dates';
import {
  LucideLanguages,
  LucideScissors,
  LucideTextQuote,
  LucideTriangleAlert,
} from '@lucide/angular';
import { InlineButtonComponent } from '@static/components/button/inline-button.component';
import { CalloutComponent } from '@static/components/callout/callout.component';
import { DocumentHeaderComponent } from '@static/components/document-header/document-header.component';
import {
  EditorComponent,
  EditorMode,
} from '@static/components/editor/editor.component';
import { MarkdownSyntax } from '@static/components/editor/editor-content';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { InlineSelectComponent } from '@static/components/inline-select/inline-select.component';
import {
  EditorSelection,
  EditorSelectionAction,
} from '@static/components/editor/editor-selection';
import { ResultCardComponent } from '@static/components/result-card/result-card.component';
import { SuggestionMenuComponent } from '@static/components/suggestion-menu/suggestion-menu.component';
import { TaskPreviewCardComponent } from '@static/components/task-preview-card/task-preview-card.component';
import { QueryEmbedComponent } from '../editor/query-embed.component';
import {
  createQueryEmbedExtension,
  QueryEmbedMount,
} from '../editor/query-embed.extension';
import { queryEmbedSyntax } from '../editor/query-embed-syntax';
import {
  basicEditorCommands,
  EditorCommand,
} from '@static/components/editor/editor-commands';
import { DialogService } from '@core/services/dialog.service';
import { QueryPickerDialogComponent } from '../dialogs/query-picker-dialog.component';
import { createPageEditorCommands } from '../editor/page-editor-commands';
import { PageReferenceController } from '../editor/page-reference.controller';
import { PageTransformController } from '../editor/page-transform.controller';
import {
  createPageReferenceExtension,
  PageReferenceDisplay,
  PageReferenceHost,
} from '../editor/page-reference.extension';
import {
  PageReferenceAttrs,
  pageReferenceSyntax,
} from '../editor/page-reference-syntax';
import { PageTreeStore } from '../services/page-tree.store';
import { PageDraft } from '../services/page-draft';
import { PageMetaChipsComponent } from './page-meta-chips.component';

// One shared instance, so the editor's cached lexer for it is built once for the whole app.
const pageSyntax: MarkdownSyntax = {
  inline: [pageReferenceSyntax],
  block: [queryEmbedSyntax],
};

interface HoveredReference {
  attrs: PageReferenceAttrs;
  element: HTMLElement;
  left: number;
  top: number;
}

const selectionActions: EditorSelectionAction[] = [
  {
    id: 'summarise',
    label: $localize`:Bubble menu action that has the assistant summarise the selected text:Summarise`,
    icon: LucideTextQuote,
  },
  {
    id: 'tighten',
    label: $localize`:Bubble menu action that has the assistant shorten the selected text:Tighten`,
    icon: LucideScissors,
  },
  {
    id: 'translate',
    label: $localize`:Bubble menu action that has the assistant translate the selected text:Translate`,
    icon: LucideLanguages,
  },
];

type SelectionTransform = Extract<
  PageTransformAction,
  'summarise' | 'tighten' | 'translate'
>;

const selectionTransforms: ReadonlySet<string> = new Set(
  selectionActions.map((action) => action.id)
);

function isSelectionTransform(id: string): id is SelectionTransform {
  return selectionTransforms.has(id);
}

// The server reads up to 20,000 characters; the end of the page matters most for what comes next.
const TEXT_ABOVE_LIMIT = 18_000;

function tailOf(markdown: string, limit: number): string {
  if (markdown.length <= limit) return markdown;

  const tail = markdown.slice(-limit);
  const blockStart = tail.indexOf('\n\n');

  return blockStart >= 0 ? tail.slice(blockStart + 2) : tail;
}

@Component({
  selector: 'app-page-editor',
  imports: [
    SuggestionMenuComponent,
    TaskPreviewCardComponent,
    PageMetaChipsComponent,
    DocumentHeaderComponent,
    EditorComponent,
    CalloutComponent,
    InlineButtonComponent,
    EmptyStateComponent,
    ResultCardComponent,
    InlineSelectComponent,
  ],
  providers: [PageReferenceController, PageTransformController],
  host: {
    class: 'block',
    '(window:wheel)': 'clearHover()',
  },
  template: `
    @if (references.open() && mode() === 'rich') {
      <app-suggestion-menu
        class="fixed z-50"
        [class.-translate-y-full]="references.position().above"
        [style.left.px]="references.position().left"
        [style.top.px]="references.position().top"
        i18n-ariaLabel="
          Accessible label for the menu of tasks and pages to mention
        "
        ariaLabel="Mention a task or page"
        i18n-emptyLabel="
          Shown in the mention menu when nothing matches what was typed
        "
        emptyLabel="Type to search tasks and pages"
        [items]="references.items()"
        [activeIndex]="references.activeIndex()"
        (picked)="references.pick($event)"
        (hovered)="references.activeIndex.set($event)" />
    }

    @if (transform.open()) {
      <app-result-card
        class="fixed z-50"
        [style.left.px]="transform.position().left"
        [style.top.px]="transform.position().top"
        [heading]="transform.heading()"
        [status]="transform.status()"
        [text]="transform.text()"
        [error]="transform.error()"
        [actions]="transform.actions()"
        i18n-loadingLabel="Shown while the assistant works on page text"
        loadingLabel="Working on it…"
        i18n-dismissLabel="
          Accessible label for closing the assistant result card
        "
        dismissLabel="Discard"
        (acted)="onTransformAccepted($event)"
        (dismissed)="transform.close()">
        @if (transform.action() === 'translate') {
          <app-inline-select
            resultCardControls
            i18n-ariaLabel="
              Accessible label for the language page text is translated into
            "
            ariaLabel="Translate into"
            [options]="transform.languageOptions"
            [value]="transform.language()"
            (changed)="transform.setLanguage($event)" />
        }
      </app-result-card>
    }

    @if (hoveredTask(); as card) {
      <app-task-preview-card
        class="pointer-events-none fixed z-50"
        [style.left.px]="card.left"
        [style.top.px]="card.top"
        [systemId]="card.task.systemId"
        [name]="card.task.name"
        [statusName]="card.task.statusName"
        [statusColor]="card.task.statusColor"
        [statusCategory]="card.task.statusCategory"
        [sprintName]="card.task.sprintName"
        [assigneeNames]="card.task.assigneeNames" />
    }

    <div
      class="mx-auto flex w-full max-w-[760px] flex-col px-12 pt-10 pb-20 max-md:px-5 max-md:pt-6">
      @if (draft().conflict()) {
        <app-callout
          class="mb-6"
          color="warn"
          role="alert"
          [icon]="conflictIcon"
          i18n-title="
            Title of the notice shown when someone else saved the page first
          "
          title="Someone else changed this page">
          <span
            i18n="
              Body of the notice shown when someone else saved the page first
            ">
            Your latest edits aren't saved. Load their version, or save yours
            over it.
          </span>
          <span class="mt-2 flex gap-3">
            <button
              app-inline-button
              type="button"
              (click)="draft().acceptTheirs()">
              <span
                i18n="
                  Button that discards local edits and loads the newer saved
                  page
                "
                >Load their version</span
              >
            </button>
            <button
              app-inline-button
              type="button"
              color="warn"
              (click)="draft().keepMine()">
              <span
                i18n="Button that saves local edits over the newer saved page"
                >Save mine</span
              >
            </button>
          </span>
        </app-callout>
      }

      @if (draft().errorMessage(); as message) {
        <app-callout
          class="mb-6"
          color="warn"
          role="alert"
          [icon]="conflictIcon">
          {{ message }}
        </app-callout>
      }

      <app-document-header
        i18n-placeholder="Placeholder in an empty page title"
        placeholder="Untitled"
        i18n-titleLabel="Accessible label for the page title"
        titleLabel="Page title"
        [title]="draft().title()"
        [byline]="byline()"
        [readOnly]="readOnly()"
        (titleChange)="draft().title.set($event)"
        (committed)="draft().commitTitle($event)">
        <app-page-meta-chips
          documentChips
          [status]="draft().latest().status"
          [tags]="draft().latest().tags"
          [canEdit]="!readOnly()"
          (tagsChanged)="onTagsChanged($event)" />
      </app-document-header>

      @if (isDocument()) {
        <app-editor
          appearance="document"
          i18n-placeholder="Placeholder in an empty page body"
          placeholder="Start writing…"
          [mode]="mode()"
          [value]="draft().body()"
          [isReadOnly]="readOnly()"
          [finalSave]="finalSave"
          [extensions]="extensions"
          [syntax]="syntax"
          [commands]="commands()"
          [selectionActions]="selectionActions()"
          (selectionAction)="onSelectionAction($event)"
          (saved)="draft().commitBody($event)" />
      } @else {
        <app-empty-state
          compact
          i18n-title="Shown on a folder, which has no text of its own"
          title="This is a folder"
          i18n-description="Hint shown on a folder page"
          description="Folders hold pages. Create a page inside it from the tree or the page menu." />
      }
    </div>
  `,
})
export class PageEditorComponent implements OnInit {
  private readonly editor = viewChild(EditorComponent);
  private readonly router = inject(Router);
  private readonly dialogs = inject(DialogService);
  private readonly appRef = inject(ApplicationRef);
  private readonly environmentInjector = inject(EnvironmentInjector);
  private readonly injector = inject(Injector);
  private readonly store = inject(PageTreeStore);
  protected readonly references = inject(PageReferenceController);

  readonly draft = input.required<PageDraft>();
  readonly links = input<PageLinks | undefined>(undefined);
  readonly mode = input<EditorMode>('rich');
  readonly readOnly = input(false);

  private readonly referenceListeners = new Set<() => void>();
  private readonly hovered = signal<HoveredReference | null>(null);

  private readonly referenceHost: PageReferenceHost = {
    display: (attrs) => this.describeReference(attrs),
    subscribe: (listener) => {
      this.referenceListeners.add(listener);

      return () => this.referenceListeners.delete(listener);
    },
    hovered: (attrs, element) => this.onReferenceHovered(attrs, element),
    unhovered: (element) => this.onReferenceUnhovered(element),
    opened: (attrs) => this.openReference(attrs),
    suggestion: this.references.handlers,
  };

  protected readonly extensions = [
    createPageReferenceExtension(this.referenceHost),
    createQueryEmbedExtension((slug, element) => {
      return this.mountQueryEmbed(slug, element);
    }),
  ];
  protected readonly syntax = pageSyntax;

  private readonly canReadQueries = hasPermission(PERMISSIONS.queries.read);
  private readonly canUseAssistant = hasPermission(PERMISSIONS.assistant.use);
  protected readonly transform = inject(PageTransformController);

  protected readonly commands = computed<readonly EditorCommand[]>(() => {
    const pageCommands = createPageEditorCommands({
      canEmbedQueries: this.canReadQueries(),
      canUseAssistant: this.canUseAssistant(),
      pickQuery: () => this.pickQuery(),
      transformAbove: (action, position) => {
        this.transformAbove(action, position);
      },
    });

    return [...basicEditorCommands, ...pageCommands];
  });

  // Rewrites land through the editor, so only someone who can edit the page is offered them.
  protected readonly selectionActions = computed<EditorSelectionAction[]>(
    () => {
      const canTransform = this.canUseAssistant() && !this.readOnly();

      return canTransform ? selectionActions : [];
    }
  );

  protected readonly hoveredTask = computed(() => {
    const hovered = this.hovered();

    if (!hovered || hovered.attrs.kind !== 'task') return null;

    const task = this.linkedTask(hovered.attrs.key);

    return task ? { task, left: hovered.left, top: hovered.top } : null;
  });

  constructor() {
    // Reference chips are drawn by the editor outside Angular, so they are told when names change.
    effect(() => {
      this.links();
      this.store.nodes();
      this.references.pickedTasks();

      untracked(() => {
        this.referenceListeners.forEach((listener) => listener());
      });
    });

    // Captured, because the page scrolls inside its own container rather than the window.
    const onScroll = () => {
      this.transform.reanchor((range) => this.anchorFor(range.to));
    };

    document.addEventListener('scroll', onScroll, true);
    inject(DestroyRef).onDestroy(() => {
      document.removeEventListener('scroll', onScroll, true);
    });
  }

  ngOnInit() {
    this.references.excludePage(this.draft().id);
  }

  protected readonly conflictIcon = LucideTriangleAlert;

  protected readonly isDocument = computed(() => {
    return this.draft().kind === PageKind.document;
  });

  protected readonly byline = computed(() => {
    const page = this.draft().latest();
    const author =
      page.modifiedByDisplayName ?? page.createdByDisplayName ?? '';
    const edited = fromNow(page.updatedAt ?? page.createdAt);
    const minutes = readingMinutes(this.draft().body());

    return $localize`:Byline under a page title naming who last edited it, when, and how long it takes to read:${author}:AUTHOR: · Edited ${edited}:EDITED: · ${minutes}:MINUTES: min read`;
  });

  // Reads the draft when it runs. Safe on the way out only because the detail view rebuilds this
  // component per page id, so a flush lands on the page it came from.
  protected readonly finalSave = (body: string) => {
    this.draft().commitBody(body);
  };

  // The embed is an Angular component living inside the editor's DOM, attached to the app so it
  // updates like any other view and torn down with its node.
  private mountQueryEmbed(slug: string, element: HTMLElement): QueryEmbedMount {
    const embed = createComponent(QueryEmbedComponent, {
      environmentInjector: this.environmentInjector,
      elementInjector: this.injector,
      hostElement: element,
    });

    embed.setInput('slug', slug);
    this.appRef.attachView(embed.hostView);

    return {
      update: (next) => embed.setInput('slug', next),
      destroy: () => {
        this.appRef.detachView(embed.hostView);
        embed.destroy();
      },
    };
  }

  protected onSelectionAction(selection: EditorSelection) {
    const editor = this.editor()?.editor();
    const action = selection.actionId;

    if (!editor || !isSelectionTransform(action)) return;

    this.transform.start({
      editor,
      pageId: this.draft().id,
      action,
      text: selection.markdown,
      range: { from: selection.from, to: selection.to },
      anchor: selection.anchor,
      placements: ['replace', 'insert'],
    });
  }

  protected onTransformAccepted(placement: string) {
    const host = this.editor();
    const accepted = this.transform.accept(placement);

    if (!host || !accepted) return;

    if (accepted.placement === 'replace') {
      host.replaceWithMarkdown(
        accepted.range.from,
        accepted.range.to,
        accepted.text
      );
      return;
    }

    host.insertMarkdownAfter(accepted.range.to, accepted.text);
  }

  private transformAbove(action: 'continue' | 'summarise', position: number) {
    const host = this.editor();
    const editor = host?.editor();

    if (!host || !editor) return;

    this.transform.start({
      editor,
      pageId: this.draft().id,
      action,
      text: tailOf(host.markdownBefore(position), TEXT_ABOVE_LIMIT),
      range: { from: position, to: position },
      anchor: host.anchorAt(position),
      placements: ['insert'],
    });
  }

  private anchorFor(position: number) {
    return this.editor()?.anchorAt(position) ?? { left: 0, top: 0 };
  }

  private pickQuery(): Promise<string | undefined> {
    return this.dialogs.openForResult<string>(QueryPickerDialogComponent, {
      width: QueryPickerDialogComponent.width,
    });
  }

  private describeReference(attrs: PageReferenceAttrs): PageReferenceDisplay {
    switch (attrs.kind) {
      case 'task': {
        const linked = this.linkedTask(attrs.key);
        const picked = this.references
          .pickedTasks()
          .get(attrs.key.toLowerCase());
        const systemId = linked?.systemId ?? picked?.systemId ?? attrs.key;
        const title = linked?.name ?? picked?.title ?? '';

        return {
          key: systemId,
          text: title,
          resolved: Boolean(linked ?? picked),
        };
      }
      case 'page': {
        const page = this.store
          .nodes()
          .find((node) => String(node.id) === attrs.key);

        return {
          key: null,
          text: page?.title ?? attrs.label ?? attrs.key,
          resolved: Boolean(page),
        };
      }
      default:
        return { key: null, text: attrs.label ?? attrs.key, resolved: true };
    }
  }

  private linkedTask(key: string): PageLinkedTask | undefined {
    const lookupKey = key.toLowerCase();

    return this.links()?.tasks.find((task) => {
      return task.systemId.toLowerCase() === lookupKey;
    });
  }

  private onReferenceHovered(attrs: PageReferenceAttrs, element: HTMLElement) {
    const rect = element.getBoundingClientRect();

    this.hovered.set({ attrs, element, left: rect.left, top: rect.bottom + 8 });
  }

  // Only the chip that owns the card may clear it, so removing some other chip leaves it alone.
  private onReferenceUnhovered(element: HTMLElement) {
    const ownsCard = this.hovered()?.element === element;

    if (ownsCard) {
      this.hovered.set(null);
    }
  }

  protected clearHover() {
    this.hovered.set(null);
  }

  private openReference(attrs: PageReferenceAttrs) {
    if (attrs.kind === 'task') {
      void this.router.navigate([
        '/',
        this.store.workspaceSlug(),
        'tasks',
        attrs.key,
      ]);

      return;
    }

    if (attrs.kind !== 'page') return;

    const page = this.store
      .nodes()
      .find((node) => String(node.id) === attrs.key);

    if (page) {
      void this.router.navigate(this.store.link(page.slug));
    }
  }

  protected onTagsChanged(tags: PageTag[]) {
    this.draft().updateMeta({ tags });
  }

  scrollToHeading(index: number) {
    this.editor()?.scrollToHeading(index);
  }
}

const wordsPerMinute = 200;

function readingMinutes(markdown: string): number {
  const words = markdown.split(/\s+/).filter(Boolean).length;

  return Math.max(1, Math.round(words / wordsPerMinute));
}
