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
import { PageHeaderTitleComponent } from './page-header-title.component';

@Component({
  selector: 'app-page-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    LucideMenu,
    FlatButtonComponent,
    PageHeaderTitleComponent,
    PageHeaderActionsComponent,
  ],
  template: `<header
    class="mb-6 flex max-h-34 flex-col pt-[0.4rem] max-[600px]:flex-row max-[600px]:items-center max-[600px]:pt-0 max-[600px]:pb-[1.4rem]">
    <div class="flex flex-row items-center justify-between max-[600px]:flex-1">
      @if (showSideNavToggle()) {
        <div>
          <button app-flat-button aria-label="Open Menu" (click)="onOpenMenu()">
            <svg lucideMenu></svg>
          </button>
        </div>
      }

      <div
        class="flex flex-col justify-between gap-8 max-[600px]:mt-1 max-[600px]:flex-1">
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
  readonly secondaryActions = input<HeaderAction[]>([]);
  readonly overflowActions = input<HeaderAction[]>([]);

  readonly actionClick = output();
  readonly titleSubmitted = output<string>();

  readonly showSideNavToggle = this.store.selectSignal(selectIsMobileView);

  onOpenMenu() {
    this.store.dispatch(openSideMenu());
  }
}
