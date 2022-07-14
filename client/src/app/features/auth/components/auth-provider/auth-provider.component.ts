import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-auth-provider',
  templateUrl: './auth-provider.component.html',
  styleUrls: ['./auth-provider.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthProviderComponent {}
