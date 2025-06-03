import { Component, Input, ChangeDetectionStrategy } from '@angular/core';

@Component({
    selector: 'app-page-container',
    templateUrl: './page-container.component.html',
    styleUrls: ['./page-container.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false
})
export class PageContainerComponent {
  @Input() verticalPadding: boolean | null = false;
  @Input() showProgress: boolean | null = false;
  @Input() marginBottom: boolean | null = false;
  @Input() fullHeight: boolean | null = true;
  @Input() centerPage: boolean | null = false;
}
