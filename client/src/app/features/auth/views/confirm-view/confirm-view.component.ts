import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
import { ActivatedRoute } from '@angular/router';
import { AuthCodeRequest } from '@app/core/store/auth/auth.models';
import { AuthCommandsService } from '@core/services/auth-commands.service';

@Component({
  selector: 'app-confirm-view',
  templateUrl: './confirm-view.component.html',
  imports: [SpinnerComponent],
})
export class ConfirmViewComponent {
  private activatedRoute = inject(ActivatedRoute);
  private auth = inject(AuthCommandsService);

  loading = this.auth.confirmEmailLoading;
  routeData = toSignal(this.activatedRoute.data);

  constructor() {
    const data = this.routeData();
    const request = data?.confirmEmail as AuthCodeRequest;

    if (!request) return;

    this.auth.confirmEmail(request);
  }
}
