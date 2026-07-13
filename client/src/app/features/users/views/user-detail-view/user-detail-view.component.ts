import { Component, inject } from '@angular/core';
import {
  selectUserDetail,
  selectUserDetailLoading,
} from '@core/store/users/users.selectors';
import { Store } from '@ngrx/store';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import { UserDetailComponent } from '../../components/user-detail/user-detail.component';

@Component({
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    PageLoadingComponent,
    UserDetailComponent,
  ],
  template: `
    <app-page-container [verticalPadding]="false" [centerPage]="true">
      <app-page-header [title]="user()?.displayName" />

      @if (loading()) {
        <app-page-loading />
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
