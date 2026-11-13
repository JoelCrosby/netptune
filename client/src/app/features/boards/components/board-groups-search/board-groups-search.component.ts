import {
  afterNextRender,
  Component,
  computed,
  inject,
  Injector,
  signal,
  viewChild,
} from '@angular/core';
import { TaskFilterService } from '@core/services/task-filter.service';
import { LucideSearch } from '@lucide/angular';
import { ToolbarButtonComponent } from '@static/components/button/toolbar-button.component';
import { SearchInputComponent } from '@static/components/search-input/search-input.component';

@Component({
  selector: 'app-board-groups-search',
  host: { class: 'contents' },
  imports: [SearchInputComponent, ToolbarButtonComponent, LucideSearch],
  template: `
    <app-search-input
      [class]="inputClass()"
      [term]="searchTerm()"
      (searchChange)="onSearch($event)" />

    <button
      app-toolbar-button
      [class]="buttonClass()"
      i18n-aria-label="Accessible label for the button that opens the task search"
      aria-label="Search tasks"
      (click)="onExpandClicked()">
      <svg lucideSearch class="h-4 w-4" aria-hidden="true"></svg>
    </button>
  `,
})
export class BoardGroupsSearchComponent {
  private readonly filters = inject(TaskFilterService);
  private readonly injector = inject(Injector);
  private readonly input = viewChild(SearchInputComponent);

  private readonly expanded = signal(false);

  readonly searchTerm = computed(() => this.filters.filters().term);

  protected readonly inputClass = computed(() => {
    return this.expanded() ? 'block' : 'hidden @4xl:block';
  });

  protected readonly buttonClass = computed(() => {
    return this.expanded() ? 'hidden' : '@4xl:hidden';
  });

  onExpandClicked() {
    this.expanded.set(true);

    afterNextRender(() => this.input()?.focus(), { injector: this.injector });
  }

  onSearch(term: string | null) {
    this.filters.update({ term });

    if (!term) {
      this.expanded.set(false);
    }
  }
}
