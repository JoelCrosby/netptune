import { Component, inject } from '@angular/core';
import { userDetailResource } from '@core/resources/user.resource';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { map } from 'rxjs/operators';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import { UserDetailComponent } from '../../components/user-detail/user-detail.component';

@Component({
  selector: 'app-user-detail-view',
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
        <app-user-detail [user]="user()" />
      }
    </app-page-container>
  `,
})
export class UserDetailViewComponent {
  private route = inject(ActivatedRoute);

  private readonly userId = toSignal(
    this.route.params.pipe(map((params) => params['id'] as string | undefined))
  );

  private readonly userResource = userDetailResource(this.userId);

  readonly user = this.userResource.value;
  readonly loading = this.userResource.isLoading;
  readonly loadError = this.userResource.error;

  reload() {
    this.userResource.reload();
  }
}
