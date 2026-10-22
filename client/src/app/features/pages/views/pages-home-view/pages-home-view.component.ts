import {
  Component,
  computed,
  ElementRef,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { Router } from '@angular/router';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import {
  CreatePageRequest,
  PageKind,
  PageReviewReason,
} from '@core/models/pages';
import { AiAssistantService } from '@core/services/ai-assistant.service';
import { DialogService } from '@core/services/dialog.service';
import { PageCommandsService } from '@core/services/page-commands.service';
import { SessionService } from '@core/services/session.service';
import { fromNow } from '@core/util/dates';
import { LucideFileClock, LucideHistory, LucideUpload } from '@lucide/angular';
import { HeaderAction } from '@core/types/header-action';
import { ChartCardComponent } from '@static/components/chart-card/chart-card.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { PageBodyComponent } from '@static/components/page-container/page-body.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { PromptHeroComponent } from '@static/components/prompt-hero/prompt-hero.component';
import { SectionHeaderComponent } from '@static/components/section-header/section-header.component';
import {
  SegmentedControlComponent,
  SegmentedOption,
} from '@static/components/segmented-control/segmented-control.component';
import { TemplateCardComponent } from '@static/components/template-card/template-card.component';
import {
  RowListComponent,
  RowListItem,
} from '@static/components/row-list/row-list.component';
import { PageTreeComponent } from '../../components/page-tree.component';
import { SprintPickerDialogComponent } from '../../dialogs/sprint-picker-dialog.component';
import {
  pagesNeedingReviewResource,
  recentPagesResource,
} from '../../resources/pages.resource';
import { PageTreeStore } from '../../services/page-tree.store';
import { PageTemplateOption, pageTemplates } from '../../util/page-templates';

const REVIEW_REASONS: Record<PageReviewReason, string> = {
  [PageReviewReason.stale]: $localize`:Why a page is listed under needs review:Not reviewed in 6 months`,
  [PageReviewReason.unvisited]: $localize`:Why a page is listed under needs review:Nobody has opened it in 90 days`,
  [PageReviewReason.brokenLinks]: $localize`:Why a page is listed under needs review:Links to a deleted task or page`,
};

@Component({
  selector: 'app-pages-home-view',
  imports: [
    PageHeaderComponent,
    PageTreeComponent,
    PromptHeroComponent,
    SegmentedControlComponent,
    SectionHeaderComponent,
    TemplateCardComponent,
    ChartCardComponent,
    EmptyStateComponent,
    PageBodyComponent,
    RowListComponent,
  ],
  host: { class: 'flex min-h-0 flex-1 flex-col' },
  template: `
    <app-page-header
      toolbar
      i18n-title="Title of the pages home"
      title="Pages"
      [count]="store.spaceNodes().length"
      [actionTitle]="canCreate() ? createLabel : null"
      [secondaryActions]="headerActions()"
      (actionClick)="onTemplate(blankTemplate)" />

    <input
      #importInput
      type="file"
      class="hidden"
      multiple
      accept=".md,.markdown,.txt,.zip"
      (change)="onImport(importInput)" />

    <app-page-body scroll>
      <div class="flex flex-col gap-8 pb-12">
        <app-page-tree class="md:hidden" />

        @if (canAsk()) {
          <app-prompt-hero
            [heading]="hero().heading"
            [description]="hero().description"
            [placeholder]="hero().placeholder"
            [hint]="hero().hint"
            [suggestions]="hero().suggestions"
            [ariaLabel]="hero().ariaLabel"
            [submitLabel]="hero().submitLabel"
            (submitted)="onPrompt($event)">
            @if (canCreate()) {
              <app-segmented-control
                promptHeroControls
                class="shrink-0"
                i18n-ariaLabel="
                  Accessible label for the switch between asking the assistant
                  and drafting a page on pages home
                "
                ariaLabel="Assistant mode"
                [options]="modeOptions"
                [(value)]="mode" />
            }
          </app-prompt-hero>
        }

        @if (canCreate()) {
          <section>
            <app-section-header
              appearance="inline"
              i18n-heading="Heading over the templates on pages home"
              heading="Start from a template"
              [description]="templatesDescription()" />
            <div
              class="grid grid-cols-7 gap-3 max-2xl:grid-cols-4 max-lg:grid-cols-2 max-sm:grid-cols-1">
              @for (template of templates; track template.key) {
                <app-template-card
                  [heading]="template.title"
                  [description]="template.description"
                  [icon]="template.icon"
                  [tone]="template.tone"
                  (activated)="onTemplate(template)" />
              }
            </div>
          </section>
        }

        @if (isPublicViewer()) {
          <app-empty-state
            class="max-md:hidden"
            outlined
            i18n-title="
              Pages home heading for someone reading a public workspace
            "
            title="Pick a page to read"
            i18n-description="
              Pages home hint for someone reading a public workspace
            "
            description="Published pages are listed in the sidebar." />
        } @else {
          <div class="grid grid-cols-1 gap-6 lg:grid-cols-2">
            <app-chart-card
              [flush]="true"
              [icon]="recentIcon"
              i18n-title="Heading over the pages the user opened recently"
              title="Recently viewed"
              i18n-description="
                Description of the pages home recently viewed card
              "
              description="Pages you opened lately">
              <app-row-list
                flush
                i18n-ariaLabel="
                  Accessible label for the list of recently viewed pages
                "
                ariaLabel="Recently viewed pages"
                i18n-emptyLabel="
                  Shown on pages home before the user has opened any page
                "
                emptyLabel="Pages you open appear here."
                [items]="recentItems()" />
            </app-chart-card>

            <app-chart-card
              [flush]="true"
              [icon]="reviewIcon"
              i18n-title="Heading over published pages that may be out of date"
              title="Needs review"
              i18n-description="Description of the pages home needs review card"
              description="Published pages that may be out of date">
              <app-row-list
                flush
                i18n-ariaLabel="
                  Accessible label for the list of pages that need review
                "
                ariaLabel="Pages that need review"
                i18n-emptyLabel="Shown when no published page needs review"
                emptyLabel="Every published page is up to date."
                [items]="reviewItems()"
                (actioned)="onReviewed($event)" />
            </app-chart-card>
          </div>
        }
      </div>
    </app-page-body>
  `,
})
export class PagesHomeViewComponent {
  protected readonly store = inject(PageTreeStore);
  private readonly commands = inject(PageCommandsService);
  private readonly router = inject(Router);
  private readonly dialogs = inject(DialogService);
  private readonly assistant = inject(AiAssistantService);
  private readonly canUseAssistant = hasPermission(PERMISSIONS.assistant.use);
  private readonly canUpdate = hasPermission(PERMISSIONS.pages.update);
  private readonly recent = recentPagesResource();
  protected readonly isPublicViewer = inject(SessionService).isPublicViewer;
  private readonly needingReview = pagesNeedingReviewResource();

  protected readonly canCreate = hasPermission(PERMISSIONS.pages.create);
  protected readonly createLabel = $localize`:Button that creates a page:Create Page`;
  protected readonly templates = pageTemplates;
  protected readonly recentIcon = LucideHistory;
  protected readonly reviewIcon = LucideFileClock;
  protected readonly blankTemplate = pageTemplates[0];

  private readonly importInput =
    viewChild.required<ElementRef<HTMLInputElement>>('importInput');

  protected readonly headerActions = computed<HeaderAction[]>(() => {
    if (!this.canCreate()) return [];

    return [
      {
        label: importLabel,
        icon: LucideUpload,
        click: () => this.importInput().nativeElement.click(),
      },
    ];
  });

  protected readonly canAsk = computed(() => {
    return this.assistant.isAvailable() && this.canUseAssistant();
  });

  protected readonly mode = signal<HeroMode>('ask');
  protected readonly modeOptions: SegmentedOption<HeroMode>[] = [
    { value: 'ask', label: askModeLabel },
    { value: 'draft', label: draftModeLabel },
  ];

  private readonly spaceName = computed(() => {
    return this.store.activeSpace()?.name ?? workspaceLabel;
  });

  protected readonly templatesDescription = computed(() => {
    return $localize`:Hint beside the templates on pages home naming the space new pages go in:Each one opens a new page in ${this.spaceName()}:SPACE:`;
  });

  private readonly documentCount = computed(() => {
    return this.store.nodes().filter((node) => node.kind === PageKind.document)
      .length;
  });

  private readonly askSuggestions = computed(() => {
    return this.recent
      .value()
      .slice(0, 3)
      .map((page) => {
        return $localize`:Suggested question on pages home about a page the user opened recently:Summarise ${page.title}:TITLE:`;
      });
  });

  protected readonly hero = computed<HeroCopy>(() => {
    if (this.mode() === 'draft') {
      return {
        heading: $localize`:Heading of the pages home box when drafting a page with the assistant:What should this page cover?`,
        description: $localize`:Description under the pages home heading when drafting a page with the assistant:Describe the page and the assistant writes a first draft for you to review.`,
        placeholder: $localize`:Placeholder of the pages home box when drafting a page:e.g. A runbook for rotating the deploy credentials`,
        hint: $localize`:Hint in the pages home box saying where an assistant draft is created:New page in Workspace`,
        suggestions: draftSuggestions,
        ariaLabel: $localize`:Accessible label of the pages home box when drafting a page:Describe the page for the assistant to draft`,
        submitLabel: $localize`:Accessible label of the button that asks the assistant to draft a page:Draft`,
      };
    }

    return {
      heading: $localize`:Heading of the pages home box that asks the assistant a question:What do you want to know?`,
      description: $localize`:Description under the pages home heading when asking the assistant:The assistant answers from your pages and links to the pages it used.`,
      placeholder: $localize`:Placeholder of the box on pages home that asks the assistant about the workspace's pages:Ask anything about your pages…`,
      hint: $localize`:Hint in the pages home box saying how many pages the assistant searches:Searching ${this.documentCount()}:COUNT: pages`,
      suggestions: this.askSuggestions(),
      ariaLabel: $localize`:Accessible label of the box on pages home that asks the assistant a question:Ask the assistant about your pages`,
      submitLabel: $localize`:Accessible label of the button that sends a question to the assistant:Ask`,
    };
  });

  protected readonly recentItems = computed<RowListItem[]>(() => {
    return this.recent.value().map((page) => {
      return {
        id: page.id,
        title: page.title,
        subtitle: this.pathOf(page.id),
        meta: [page.updatedByDisplayName ?? '', fromNow(page.visitedAt)],
        link: this.store.link(page.slug),
      };
    });
  });

  protected readonly reviewItems = computed<RowListItem[]>(() => {
    const actionLabel = this.canUpdate() ? markReviewedLabel : null;

    return this.needingReview.value().map((page) => {
      return {
        id: page.id,
        title: page.title,
        subtitle: REVIEW_REASONS[page.reason],
        link: this.store.link(page.slug),
        actionLabel,
      };
    });
  });

  protected onPrompt(text: string) {
    if (this.mode() === 'draft') {
      this.assistant.draftPage(text);
      return;
    }

    this.assistant.askAcrossPages(text);
  }

  protected async onTemplate(template: PageTemplateOption) {
    const request = await this.requestFor(template);

    if (!request) return;

    this.commands.create(request).subscribe((page) => {
      void this.router.navigate(this.store.link(page.slug));
    });
  }

  protected onImport(input: HTMLInputElement) {
    const files = Array.from(input.files ?? []);

    input.value = '';

    if (!files.length) return;

    this.commands
      .importPages(files, this.store.activeSpaceId())
      .subscribe((result) => {
        if (result.firstSlug) {
          void this.router.navigate(this.store.link(result.firstSlug));
        }
      });
  }

  protected onReviewed(id: string | number) {
    this.commands.markReviewed(Number(id));
  }

  private async requestFor(
    template: PageTemplateOption
  ): Promise<CreatePageRequest | null> {
    const isFromSprint = template.key === 'from-sprint';

    const projectId = this.store.activeSpaceId();

    if (!isFromSprint) {
      return { kind: PageKind.document, templateKey: template.key, projectId };
    }

    const sprintId = await this.dialogs.openForResult<number>(
      SprintPickerDialogComponent,
      {
        width: SprintPickerDialogComponent.width,
      }
    );

    if (sprintId === undefined) return null;

    return {
      kind: PageKind.document,
      templateKey: template.key,
      sprintId,
      projectId,
    };
  }

  private pathOf(id: number): string | null {
    const ancestors = this.store.ancestorsOf(id);

    if (!ancestors.length) return null;

    return ancestors.map((node) => node.title).join(' / ');
  }
}

const markReviewedLabel = $localize`:Button that marks a page as still accurate so it leaves needs review:Mark reviewed`;

const importLabel = $localize`:Button on pages home that imports markdown files as pages:Import`;

const workspaceLabel = $localize`:Name of the workspace's own page space, outside any project:Workspace`;

const askModeLabel = $localize`:Option on pages home that asks the assistant a question:Ask`;

const draftModeLabel = $localize`:Option on pages home that has the assistant draft a new page:Draft a page`;

const draftSuggestions = [
  $localize`:Suggested page for the assistant to draft on pages home:Release notes for the current sprint`,
  $localize`:Suggested page for the assistant to draft on pages home:Onboarding guide for new team members`,
  $localize`:Suggested page for the assistant to draft on pages home:Postmortem for a production incident`,
];

type HeroMode = 'ask' | 'draft';

interface HeroCopy {
  heading: string;
  description: string;
  placeholder: string;
  hint: string;
  suggestions: readonly string[];
  ariaLabel: string;
  submitLabel: string;
}
