import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
    templateUrl: './settings-view.component.html',
    styleUrls: ['./settings-view.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false
})
export class SettingsViewComponent {}
