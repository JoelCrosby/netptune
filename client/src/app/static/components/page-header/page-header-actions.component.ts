import {
  Component,
  booleanAttribute,
  computed,
  input,
  output,
} from '@angular/core';
import { HeaderAction } from '@core/types/header-action';
import { LucideDynamicIcon, LucideEllipsis } from '@lucide/angular';
import { FlatButtonComponent } from '../button/flat-button.component';
import { DropdownMenuComponent } from '../dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '../dropdown-menu/menu-item.component';
import { StrokedButtonComponent } from '../button/stroked-button.component';

@Component({
  selector: 'app-page-header-actions',
  template: `
    <div [class]="containerClass()">
      @for (action of secondaryActions(); track action) {
        <button
          app-flat-button
          class="ml-3 rounded-[6rem]"
          (click)="action.click && action.click()">
          {{ action.label }}
        </button>
      }
      @if (overflowActions().length) {
        <button
          app-stroked-button
          [class]="overflowButtonClass()"
          i18n-aria-label="
            Accessible label for the button that opens the page action menu
          "
          aria-label="Actions"
          (click)="menu.toggle($any($event.currentTarget))">
          <svg lucideEllipsis></svg>
        </button>
        <app-dropdown-menu #menu xPosition="before">
          @for (action of overflowActions(); track action) {
            <button
              app-menu-item
              (click)="action.click && action.click(); menu.close()">
              @if (action.icon) {
                <svg
                  [lucideIcon]="action.icon"
                  class="h-4 w-4"
                  aria-hidden="true"></svg>
              }
              {{ action.label }}
            </button>
          }
        </app-dropdown-menu>
      }

      @if (actionTitle()) {
        <button
          app-flat-button
          [class]="primaryButtonClass()"
          (click)="actionClick.emit()">
          {{ actionTitle() }}
        </button>
      }

      <ng-content />
    </div>
  `,
  imports: [
    LucideEllipsis,
    LucideDynamicIcon,
    FlatButtonComponent,
    StrokedButtonComponent,
    DropdownMenuComponent,
    MenuItemComponent,
  ],
})
export class PageHeaderActionsComponent {
  readonly actionTitle = input<string | null>();
  readonly secondaryActions = input<HeaderAction[]>([]);
  readonly overflowActions = input<HeaderAction[]>([]);

  readonly compact = input(false, { transform: booleanAttribute });

  readonly actionClick = output();

  protected readonly containerClass = computed(() => {
    const base = 'flex flex-row flex-wrap items-center';

    return this.compact() ? `${base} gap-2` : `${base} gap-4`;
  });

  protected readonly overflowButtonClass = computed(() => {
    if (!this.compact()) return '';

    return 'h-[34px] w-[34px] min-w-0 rounded px-0 text-foreground/75';
  });

  protected readonly primaryButtonClass = computed(() => {
    if (!this.compact()) return '';

    return 'h-[34px] min-w-0 rounded px-4 text-[13.5px] tracking-[0.02em]';
  });
}
