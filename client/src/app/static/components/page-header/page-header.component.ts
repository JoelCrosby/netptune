import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
  output,
} from '@angular/core';
import { openSideMenu } from '@core/store/layout/layout.actions';
import { selectIsMobileView } from '@core/store/layout/layout.selectors';
import { HeaderAction } from '@core/types/header-action';
import { LucideMenu } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { FlatButtonComponent } from '../button/flat-button.component';
import { PageHeaderActionsComponent } from './page-header-actions.component';
import { PageHeaderBackLinkComponent } from './page-header-back-link.component';
import { PageHeaderTitleComponent } from './page-header-title.component';

@Component({
  selector: 'app-page-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    LucideMenu,
    FlatButtonComponent,
    PageHeaderBackLinkComponent,
    PageHeaderTitleComponent,
    PageHeaderActionsComponent,
  ],
  template: `<header
    class="flex h-[136px] max-h-[136px] flex-col pt-[0.4rem] max-[600px]:flex-row max-[600px]:items-center max-[600px]:pt-0 max-[600px]:pb-[1.4rem]">
    <div
      class="flex h-[108px] flex-row items-center justify-between max-[600px]:flex-1">
      @if (showSideNavToggle()) {
        <div>
          <button app-flat-button aria-label="Open Menu" (click)="onOpenMenu()">
            <svg lucideMenu></svg>
          </button>
        </div>
      }

      <div
        class="flex h-full flex-col justify-between max-[600px]:mt-1 max-[600px]:flex-1">
        <div class="h-6">
          <app-page-header-back-link
            [backLink]="backLink()"
            [backLabel]="backLabel()" />
        </div>

        <app-page-header-title
          [title]="title()"
          [titleEditable]="titleEditable()"
          (titleSubmitted)="titleSubmitted.emit($event)">
          <ng-content />
        </app-page-header-title>
      </div>

      <app-page-header-actions
        [secondaryActions]="secondaryActions()"
        [overflowActions]="overflowActions()"
        [actionTitle]="actionTitle()"
        (actionClick)="actionClick.emit()" />
    </div>
  </header> `,
})
export class PageHeaderComponent {
  private store = inject(Store);

  readonly title = input<string | null>();
  readonly titleEditable = input(false);
  readonly actionTitle = input<string | null>();
  readonly backLink = input<string[] | number[] | null>();
  readonly backLabel = input<string | null>();
  readonly secondaryActions = input<HeaderAction[]>([]);
  readonly overflowActions = input<HeaderAction[]>([]);

  readonly actionClick = output();
  readonly titleSubmitted = output<string>();

  readonly showSideNavToggle = this.store.selectSignal(selectIsMobileView);

  onOpenMenu() {
    this.store.dispatch(openSideMenu());
  }
}
