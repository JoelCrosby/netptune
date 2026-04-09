import {
  ChangeDetectionStrategy,
  Component,
  HostBinding,
  input,
} from '@angular/core';
import { MatIcon } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { HeaderAction } from '@core/types/header-action';
import { FlatButtonComponent } from '../button/flat-button.component';
import { ButtonLinkComponent } from '../button/button-link.component';

@Component({
  selector: 'app-card-actions',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, MatIcon, FlatButtonComponent, ButtonLinkComponent],
  template: `
    <div class="mt-4 flex items-center gap-2">
      @for (
        action of actions();
        track action.routerLink;
        let isFirst = $first
      ) {
        @if (action.isLink) {
          @if (isFirst) {
            <a
              app-flat-button
              color="primary"
              class="mr-3"
              type="button"
              [routerLink]="action.routerLink">
              @if (action.icon) {
                <mat-icon class="mr-[0.6rem]">
                  {{ action.icon }}
                </mat-icon>
              }
              {{ action.label }}
            </a>
          } @else {
            <a
              app-button-link
              color="primary"
              class="mr-3"
              type="button"
              [routerLink]="action.routerLink">
              @if (action.icon) {
                <mat-icon class="mr-[0.6rem]">
                  {{ action.icon }}
                </mat-icon>
              }
              {{ action.label }}
            </a>
          }
        } @else {
          @if (isFirst) {
            <button
              app-flat-button
              color="primary"
              class="mr-3"
              (click)="action.click && action.click()">
              @if (action.icon) {
                <mat-icon class="mr-[0.6rem]">
                  {{ action.icon }}
                </mat-icon>
              }
              {{ action.label }}
            </button>
          } @else {
            <button
              app-button-link
              color="primary"
              class="mr-3"
              (click)="action.click && action.click()">
              @if (action.icon) {
                <mat-icon class="mr-[0.6rem]">
                  {{ action.icon }}
                </mat-icon>
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
  @HostBinding('class') className = 'netp-card-actions';

  readonly actions = input<HeaderAction[]>([]);
}
