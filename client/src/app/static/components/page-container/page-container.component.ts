import { Component, Input, ChangeDetectionStrategy } from '@angular/core';

import { MatProgressBar } from '@angular/material/progress-bar';

@Component({
    selector: 'app-page-container',
    templateUrl: './page-container.component.html',
    styleUrls: ['./page-container.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MatProgressBar]
})
export class PageContainerComponent {
  @Input() verticalPadding: boolean | null = false;
  @Input() showProgress: boolean | null = false;
  @Input() marginBottom: boolean | null = false;
  @Input() fullHeight: boolean | null = true;
  @Input() centerPage: boolean | null = false;
}
