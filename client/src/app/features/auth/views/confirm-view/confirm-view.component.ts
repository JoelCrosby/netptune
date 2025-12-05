import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { ActivatedRoute } from '@angular/router';
import { confirmEmail } from '@core/auth/store/auth.actions';
import { AuthCodeRequest } from '@core/auth/store/auth.models';
import { selectIsConfirmEmailLoading } from '@core/auth/store/auth.selectors';
import { Store } from '@ngrx/store';

@Component({
  templateUrl: './confirm-view.component.html',
  styleUrls: ['./confirm-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatProgressSpinner],
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
