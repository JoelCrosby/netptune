import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { openSideMenu } from '@core/store/layout/layout.actions';
import { selectIsMobileView } from '@core/store/layout/layout.selectors';
import { HeaderAction } from '@core/types/header-action';
import { Store } from '@ngrx/store';
import { AsyncPipe } from '@angular/common';
import { MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { InlineEditInputComponent } from '../inline-edit-input/inline-edit-input.component';
import { MatRipple } from '@angular/material/core';
import { MatMenuTrigger, MatMenu, MatMenuContent, MatMenuItem } from '@angular/material/menu';

@Component({
    selector: 'app-page-header',
    templateUrl: './page-header.component.html',
    styleUrls: ['./page-header.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MatIconButton, MatIcon, RouterLink, InlineEditInputComponent, MatRipple, MatMenuTrigger, MatMenu, MatMenuContent, MatMenuItem, AsyncPipe]
})
export class PageHeaderComponent {
  private store = inject(Store);

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

  onOpenMenu() {
    this.store.dispatch(openSideMenu());
  }
}
