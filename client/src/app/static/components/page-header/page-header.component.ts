import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  Output,
} from '@angular/core';
import { openSideMenu } from '@core/store/layout/layout.actions';
import { selectIsMobileView } from '@core/store/layout/layout.selectors';
import { HeaderAction } from '@core/types/header-action';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-page-header',
  templateUrl: './page-header.component.html',
  styleUrls: ['./page-header.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PageHeaderComponent {
  @Input() title?: string | null;
  @Input() titleEditable = false;
  @Input() actionTitle?: string | null;
  @Input() backLink?: string[] | number[] | null;
  @Input() backLabel?: string | null;
  @Input() secondaryActions: HeaderAction[] = [];
  @Input() overflowActions: HeaderAction[] = [];

  @Output() actionClick = new EventEmitter();
  @Output() titleSubmitted = new EventEmitter<string>();

  showSideNavToggle$ = this.store.select(selectIsMobileView);

  constructor(private store: Store) {}

  onOpenMenu() {
    this.store.dispatch(openSideMenu());
  }
}
