import {
  selectUserDetail,
  selectUserDetailLoading,
} from '@core/store/users/users.selectors';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Store } from '@ngrx/store';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
import { UserDetailComponent } from '../../components/user-detail/user-detail.component';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    SpinnerComponent,
    UserDetailComponent,
  ],
  template: `
    <app-page-container
      [verticalPadding]="false"
      [fullHeight]="true"
      [centerPage]="true"
      [marginBottom]="true">
      <app-page-header
        [title]="user()?.displayName"
        backLabel="Back to Users"
        [backLink]="['../../users']" />

      @if (loading()) {
        <div class="flex h-full flex-col items-center justify-center">
          <app-spinner diameter="32px" />
        </div>
      } @else {
        <app-user-detail />
      }
    </app-page-container>
  `,
})
export class UserDetailViewComponent {
  private store = inject(Store);

  loading = this.store.selectSignal(selectUserDetailLoading);
  user = this.store.selectSignal(selectUserDetail);
}
