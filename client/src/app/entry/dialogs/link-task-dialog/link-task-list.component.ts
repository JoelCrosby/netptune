import {
  Component,
  computed,
  effect,
  input,
  linkedSignal,
  output,
  signal,
  untracked,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { PERMISSIONS } from '@core/auth/permissions';
import { ClientResponse } from '@core/models/client-response';
import { Page } from '@core/models/pagination';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { permissionResource } from '@core/resources/permission.resource';
import { LucideCheck, LucideSearch } from '@lucide/angular';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { SkeletonComponent } from '@static/components/skeleton/skeleton.component';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { TaskStatusPillComponent } from '@static/components/task-status-pill.component';
import { debounceTime, map } from 'rxjs/operators';

const PAGE_SIZE = 50;

// How close to the end of the scroller the next page starts loading.
const LOAD_MORE_THRESHOLD = 160;

interface LoadedTasks {
  items: readonly TaskViewModel[];
  total: number;
}

interface TaskRow {
  task: TaskViewModel;
  selected: boolean;
  linked: boolean;
}

const noTasks: LoadedTasks = { items: [], total: 0 };

@Component({
  selector: 'app-link-task-list',
  imports: [
    BadgeComponent,
    FormInputComponent,
    LucideCheck,
    SkeletonComponent,
    SpinnerComponent,
    TaskScopeIdComponent,
    TaskStatusPillComponent,
  ],
  host: { class: 'flex min-h-0 flex-col gap-3' },
  template: `
    <app-form-input
      class="flex-none"
      name="link-task-search"
      i18n-placeholder="Placeholder in the box for finding tasks to link"
      placeholder="Search tasks by name, key or tag"
      [noMargin]="true"
      [icon]="searchIcon"
      [hint]="countLabel()"
      [value]="searchInput()"
      (valueChange)="searchInput.set($event)" />

    <div
      class="border-border custom-scroll h-96 overflow-x-hidden overflow-y-auto rounded-md border lg:h-auto lg:min-h-0 lg:flex-1"
      (scroll)="onScroll($event)">
      @if (showSkeleton()) {
        @for (row of skeletonRows; track row) {
          <div
            class="border-border/70 flex items-center gap-3 border-b px-3 py-3">
            <app-skeleton class="h-4 w-4 shrink-0 rounded-sm" />
            <app-skeleton class="h-4 w-14 shrink-0" />
            <app-skeleton class="h-4 flex-1" />
            <app-skeleton class="h-4 w-16 shrink-0 rounded-full" />
          </div>
        }
      } @else {
        @for (row of rows(); track row.task.id) {
          <button
            type="button"
            class="border-border/70 focus-visible:ring-primary flex w-full items-center gap-3 border-b border-l-2 px-3 py-2.5 text-left focus-visible:ring-2 focus-visible:-outline-offset-2 focus-visible:outline-none"
            [class]="rowClass(row)"
            [disabled]="row.linked"
            [attr.aria-pressed]="row.linked ? null : row.selected"
            (click)="toggled.emit(row.task)">
            <span
              class="flex h-4 w-4 shrink-0 items-center justify-center rounded-[3px] border-2 transition-colors"
              [class]="checkboxClass(row)">
              @if (row.selected) {
                <svg
                  lucideCheck
                  strokeWidth="4"
                  class="h-3 w-3 text-white dark:text-black"
                  aria-hidden="true"></svg>
              }
            </span>

            <app-task-scope-id class="shrink-0" [id]="row.task.systemId" />

            <span class="min-w-0 flex-1 truncate text-sm">
              {{ row.task.name }}
            </span>

            @if (row.linked) {
              <app-badge class="shrink-0">
                <ng-container i18n="Marks a task that is already linked">
                  Linked
                </ng-container>
              </app-badge>
            } @else {
              <app-task-status-pill
                class="shrink-0"
                [name]="row.task.statusName"
                [color]="row.task.statusColor"
                [category]="row.task.statusCategory" />
            }
          </button>
        } @empty {
          <p class="text-muted px-3 py-8 text-center text-sm">
            <ng-container i18n="Shown when no tasks match the link search">
              No tasks match your search.
            </ng-container>
          </p>
        }

        @if (loadingMore()) {
          <div class="flex items-center justify-center gap-2 px-3 py-3">
            <app-spinner diameter="1rem" />
            <span class="text-muted text-xs" i18n="Shown while more rows load">
              Loading more tasks…
            </span>
          </div>
        } @else if (canLoadMore()) {
          <button
            type="button"
            class="text-primary hover:bg-hover focus-visible:ring-primary w-full cursor-pointer px-3 py-3 text-sm font-medium focus-visible:ring-2 focus-visible:outline-none"
            i18n="Button that fetches the next page of tasks"
            (click)="loadMore()">
            Load more tasks
          </button>
        }
      }
    </div>
  `,
})
export class LinkTaskListComponent {
  readonly excludeTaskId = input<number | undefined>();
  readonly selectedIds = input<ReadonlySet<number>>(new Set<number>());
  readonly linkedIds = input<ReadonlySet<number>>(new Set<number>());

  readonly toggled = output<TaskViewModel>();

  protected readonly searchIcon = LucideSearch;
  protected readonly skeletonRows = [1, 2, 3, 4, 5, 6, 7, 8];

  readonly searchInput = signal('');

  // Debounce so each keystroke doesn't trigger a server fetch.
  private readonly search = toSignal(
    toObservable(this.searchInput).pipe(
      debounceTime(250),
      map((value) => value.trim())
    ),
    { initialValue: '' }
  );

  // Every field the query depends on, so a change to any of them starts a fresh list.
  private readonly listKey = computed(
    () => `${this.excludeTaskId() ?? ''}:${this.search()}`
  );

  private readonly page = linkedSignal({
    source: this.listKey,
    computation: () => 1,
  });

  private readonly tasks = permissionResource<
    Page<TaskViewModel> | undefined,
    ClientResponse<Page<TaskViewModel>>
  >({
    permission: PERMISSIONS.tasks.read,
    request: () => {
      const search = this.search();
      const excludeTaskId = this.excludeTaskId();

      return {
        url: 'api/tasks',
        params: {
          page: this.page(),
          pageSize: PAGE_SIZE,
          ...(excludeTaskId ? { excludeTaskId } : {}),
          ...(search ? { search } : {}),
        },
      };
    },
    parse: (response) => response.payload,
  });

  // The resource holds one page at a time, so pages are stacked here and thrown
  // away as a set whenever the query behind them changes.
  private readonly loaded = linkedSignal<string, LoadedTasks>({
    source: this.listKey,
    computation: () => noTasks,
  });

  protected readonly rows = computed<TaskRow[]>(() => {
    const selectedIds = this.selectedIds();
    const linkedIds = this.linkedIds();

    return this.loaded().items.map((task) => ({
      task,
      selected: selectedIds.has(task.id),
      linked: linkedIds.has(task.id),
    }));
  });

  protected readonly canLoadMore = computed(() => {
    const { items, total } = this.loaded();

    return items.length < total;
  });

  protected readonly loadingMore = computed(() => {
    return this.tasks.isLoading() && this.loaded().items.length > 0;
  });

  protected readonly showSkeleton = computed(() => {
    return this.tasks.isLoading() && this.loaded().items.length === 0;
  });

  protected readonly countLabel = computed(() => {
    const { items, total } = this.loaded();

    return $localize`:Counts the task rows loaded so far out of the matching total:Showing ${items.length}:SHOWN: of ${total}:TOTAL: tasks`;
  });

  constructor() {
    effect(() => {
      const page = this.tasks.value();

      if (!page) return;

      untracked(() => {
        this.loaded.update((loaded) => append(loaded, page));
      });
    });
  }

  protected rowClass(row: TaskRow) {
    if (row.linked) {
      return 'border-l-transparent cursor-not-allowed opacity-55';
    }

    if (row.selected) {
      return 'border-l-primary bg-primary/8 hover:bg-primary/12 cursor-pointer';
    }

    return 'border-l-transparent hover:bg-hover cursor-pointer';
  }

  protected checkboxClass(row: TaskRow) {
    if (row.linked) return 'border-foreground/25 bg-foreground/10';

    return row.selected
      ? 'border-primary bg-primary'
      : 'border-foreground/40 bg-transparent';
  }

  protected onScroll(event: Event) {
    const element = event.target as HTMLElement;
    const remaining =
      element.scrollHeight - element.scrollTop - element.clientHeight;

    if (remaining > LOAD_MORE_THRESHOLD) return;

    this.loadMore();
  }

  protected loadMore() {
    if (this.tasks.isLoading() || !this.canLoadMore()) return;

    this.page.update((page) => page + 1);
  }
}

function append(loaded: LoadedTasks, page: Page<TaskViewModel>): LoadedTasks {
  const seen = new Set(loaded.items.map((task) => task.id));
  const added = page.items.filter((task) => !seen.has(task.id));

  return {
    items: added.length ? [...loaded.items, ...added] : loaded.items,
    total: page.totalCount,
  };
}
