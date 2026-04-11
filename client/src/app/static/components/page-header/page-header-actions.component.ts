import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import {
  MatMenu,
  MatMenuContent,
  MatMenuItem,
  MatMenuTrigger,
} from '@angular/material/menu';
import { HeaderAction } from '@core/types/header-action';
import { LucideDynamicIcon, LucideEllipsis } from '@lucide/angular';
import { FlatButtonComponent } from '../button/flat-button.component';

@Component({
  selector: 'app-page-header-actions',
  template: `
    <div class="flex flex-row items-center gap-4">
      @for (action of secondaryActions(); track action) {
      <button app-flat-button
        class="rounded-[6rem] ml-3"
        (click)="action.click && action.click()"
      >
        {{ action.label }}
      </button>
      } @if (overflowActions().length) {
      <button app-flat-button
        aria-label="Actions"
        [matMenuTriggerFor]="overflowMenu"
      >
        <svg lucideEllipsis></svg>
      </button>
      }

      <mat-menu #overflowMenu="matMenu" xPosition="before">
        <ng-template matMenuContent>
          @for (action of overflowActions(); track action) {
          <button mat-menu-item (click)="action.click && action.click()">
            @if (action.icon) {
              <svg [lucideIcon]="action.icon" class="mr-2 h-4 w-4" aria-hidden="true"></svg>
            }
            <span>{{ action.label }}</span>
          </button>
          }
        </ng-template>
      </mat-menu>

      @if (actionTitle()) {
      <button app-flat-button
        (click)="actionClick.emit()"
      >
        {{ actionTitle() }}
      </button>
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    LucideEllipsis,
    LucideDynamicIcon,
    MatMenuTrigger,
    MatMenu,
    MatMenuContent,
    MatMenuItem,
    FlatButtonComponent,
  ],
})
export class PageHeaderActionsComponent {
  readonly actionTitle = input<string | null>();
  readonly secondaryActions = input<HeaderAction[]>([]);
  readonly overflowActions = input<HeaderAction[]>([]);

  readonly actionClick = output();
}
