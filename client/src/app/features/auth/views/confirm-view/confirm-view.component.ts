import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
import { ActivatedRoute } from '@angular/router';
import { confirmEmail } from '@app/core/store/auth/auth.actions';
import { AuthCodeRequest } from '@app/core/store/auth/auth.models';
import { selectIsConfirmEmailLoading } from '@app/core/store/auth/auth.selectors';
import { Store } from '@ngrx/store';

@Component({
  templateUrl: './confirm-view.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [SpinnerComponent],
})
export class ConfirmViewComponent {
  private activatedRoute = inject(ActivatedRoute);
  private store = inject(Store);

  loading = this.store.selectSignal(selectIsConfirmEmailLoading);
  routeData = toSignal(this.activatedRoute.data);

  constructor() {
    const data = this.routeData();
    const request = data?.confirmEmail as AuthCodeRequest;

    if (!request) return;

    this.store.dispatch(confirmEmail({ request }));
  }
}
