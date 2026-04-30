import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { LucideDynamicIcon } from '@lucide/angular';
import { RouterLink } from '@angular/router';
import { HeaderAction } from '@core/types/header-action';
import { FlatButtonComponent } from '../button/flat-button.component';
import { ButtonLinkComponent } from '../button/button-link.component';

@Component({
  selector: 'app-card-actions',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    LucideDynamicIcon,
    FlatButtonComponent,
    ButtonLinkComponent,
  ],
  host: { class: 'netp-card-actions' },
  template: `
    <div class="mt-4 flex items-center gap-2">
      @for (action of actions(); track action.label; let isFirst = $first) {
        @if (action.isLink) {
          @if (isFirst) {
            <a
              app-flat-button
              color="primary"
              type="button"
              (click)="$event.stopPropagation()"
              [routerLink]="action.routerLink">
              @if (action.icon) {
                <svg
                  [lucideIcon]="action.icon"
                  class="mr-[0.6rem] h-4 w-4"></svg>
              }
              {{ action.label }}
            </a>
          } @else {
            <a
              app-button-link
              color="primary"
              type="button"
              (click)="$event.stopPropagation()"
              [routerLink]="action.routerLink">
              @if (action.icon) {
                <svg
                  [lucideIcon]="action.icon"
                  class="mr-[0.6rem] h-4 w-4"></svg>
              }
              {{ action.label }}
            </a>
          }
        } @else {
          @if (isFirst) {
            <button
              app-flat-button
              color="primary"
              (click)="
                $event.stopPropagation(); action.click && action.click()
              ">
              @if (action.icon) {
                <svg
                  [lucideIcon]="action.icon"
                  class="mr-[0.6rem] h-4 w-4"></svg>
              }
              {{ action.label }}
            </button>
          } @else {
            <button
              app-button-link
              color="primary"
              (click)="
                $event.stopPropagation(); action.click && action.click()
              ">
              @if (action.icon) {
                <svg
                  [lucideIcon]="action.icon"
                  class="mr-[0.6rem] h-4 w-4"></svg>
              }
              {{ action.label }}
            </button>
          }
        }
      }
    </div>
  `,
})
export class CardActionsComponent {
  readonly actions = input<HeaderAction[]>([]);
}
