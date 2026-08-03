import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-personal-settings-view',
  imports: [RouterOutlet],
  template: ` <router-outlet /> `,
})
export class PersonalSettingsViewComponent {}
