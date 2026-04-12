import {
  ChangeDetectionStrategy,
  Component,
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
    <div class="flex flex-row items-center gap-4">
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
        <button app-flat-button (click)="actionClick.emit()">
          {{ actionTitle() }}
        </button>
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
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

  readonly actionClick = output();
}
