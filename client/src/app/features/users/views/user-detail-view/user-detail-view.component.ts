import { Component, inject } from '@angular/core';
import { loadUser } from '@core/store/users/users.actions';
import {
  selectUserDetail,
  selectUserDetailLoading,
  selectUserDetailLoadingError,
} from '@core/store/users/users.selectors';
import { Store } from '@ngrx/store';
import { ActivatedRoute } from '@angular/router';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import { UserDetailComponent } from '../../components/user-detail/user-detail.component';

@Component({
  imports: [
    ErrorStateComponent,
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
      } @else if (loadError()) {
        <app-error-state
          i18n-title="Shown when a member fails to load"
          title="This member could not be loaded"
          i18n-description="Explains why a member may fail to load"
          description="They may have been removed from the workspace, or the request failed."
          (retry)="reload()" />
      } @else {
        <app-user-detail />
      }
    </app-page-container>
  `,
})
export class UserDetailViewComponent {
  private store = inject(Store);
  private route = inject(ActivatedRoute);

  loading = this.store.selectSignal(selectUserDetailLoading);
  loadError = this.store.selectSignal(selectUserDetailLoadingError);
  user = this.store.selectSignal(selectUserDetail);

  reload() {
    const userId = this.route.snapshot.params['id'] as string | undefined;

    if (!userId) return;

    this.store.dispatch(loadUser.init({ userId }));
  }
}
