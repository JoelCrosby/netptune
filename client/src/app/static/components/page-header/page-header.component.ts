import {
  Component,
  booleanAttribute,
  computed,
  inject,
  input,
  output,
} from '@angular/core';
import { HeaderAction } from '@core/types/header-action';
import { PageContainerComponent } from '../page-container/page-container.component';
import { PageHeaderActionsComponent } from './page-header-actions.component';
import { PageHeaderTitleComponent } from './page-header-title.component';

@Component({
  selector: 'app-page-header',
  imports: [PageHeaderTitleComponent, PageHeaderActionsComponent],
  template: `
    <header [class]="headerClass()">
      <div [class]="titleRowClass()">
        <div [class]="titleWrapClass()">
          <app-page-header-title
            [title]="title()"
            [titleEditable]="titleEditable()"
            [logoUrl]="logoUrl()"
            [count]="count()"
            [compact]="toolbar()"
            (titleSubmitted)="titleSubmitted.emit($event)">
            <ng-content select="[titleSuffix]" ngProjectAs="[titleSuffix]" />
            <ng-content />
          </app-page-header-title>
        </div>

        <app-page-header-actions
          [class]="actionsClass()"
          [secondaryActions]="secondaryActions()"
          [overflowActions]="overflowActions()"
          [actionTitle]="actionTitle()"
          [compact]="toolbar()"
          (actionClick)="actionClick.emit()">
          <ng-content select="[pageHeaderActions]" />
        </app-page-header-actions>
      </div>

      <div
        [class]="filterRowClass()"
        [attr.role]="filtersLabel() ? 'group' : null"
        [attr.aria-label]="filtersLabel()">
        <ng-content select="[pageHeaderFilters]" />
      </div>
    </header>
  `,
})
export class PageHeaderComponent {
  readonly title = input<string | null>();
  readonly titleEditable = input(false);
  readonly count = input<number | null>();
  readonly actionTitle = input<string | null>();
  readonly secondaryActions = input<HeaderAction[]>([]);
  readonly overflowActions = input<HeaderAction[]>([]);
  readonly logoUrl = input<string | null>(null);

  readonly toolbar = input(false, { transform: booleanAttribute });
  readonly filtersLabel = input<string | null>(null);

  readonly actionClick = output();
  readonly titleSubmitted = output<string>();

  private readonly container = inject(PageContainerComponent, {
    optional: true,
  });

  private readonly rowWidthClass = computed(() => {
    return this.container?.constrainListContent()
      ? 'mx-auto w-full max-w-[1360px]'
      : '';
  });

  protected readonly headerClass = computed(() => {
    if (this.toolbar()) {
      return 'border-border flex shrink-0 flex-col border-b';
    }

    return 'mb-6 flex max-h-34 flex-col pt-[0.4rem] max-md:flex-row max-md:items-center max-md:pt-0 max-md:pb-[1.4rem]';
  });

  protected readonly titleRowClass = computed(() => {
    const base = 'flex flex-row items-center justify-between';

    if (this.toolbar()) {
      return `${base} ${this.rowWidthClass()} gap-x-3 gap-y-2 px-8 pt-3.5 pb-2.5 max-md:flex-wrap max-md:px-3 max-md:pt-3 max-md:pb-2`;
    }

    return `${base} max-md:flex-1`;
  });

  protected readonly actionsClass = computed(() => {
    return this.toolbar() ? 'shrink-0' : '';
  });

  protected readonly titleWrapClass = computed(() => {
    if (this.toolbar()) {
      return 'flex min-w-0 flex-1 flex-col';
    }

    return 'flex flex-col justify-between gap-8 max-md:mt-1 max-md:flex-1';
  });

  protected readonly filterRowClass = computed(() => {
    if (!this.toolbar()) return 'hidden';

    return `flex flex-row flex-wrap items-center ${this.rowWidthClass()} gap-2.5 px-8 pb-3 empty:hidden max-md:px-3 max-md:pb-2.5`;
  });
}
