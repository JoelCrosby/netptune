import { Component, Input, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-page-container',
  templateUrl: './page-container.component.html',
  styleUrls: ['./page-container.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PageContainerComponent {
  @Input() verticalPadding = true;
  @Input() showProgress = false;
  @Input() marginBottom = true;
  @Input() fullHeight = false;
}
