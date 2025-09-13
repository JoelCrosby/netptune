import { ChangeDetectionStrategy, Component, Input, inject, input, output } from '@angular/core';
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

  readonly title = input<string | null>();
  readonly titleEditable = input(false);
  @Input() actionTitle?: string | null;
  @Input() backLink?: string[] | number[] | null;
  readonly backLabel = input<string | null>();
  readonly secondaryActions = input<HeaderAction[]>([]);
  readonly overflowActions = input<HeaderAction[]>([]);

  readonly actionClick = output();
  readonly titleSubmitted = output<string>();

  showSideNavToggle$ = this.store.select(selectIsMobileView);

  onOpenMenu() {
    this.store.dispatch(openSideMenu());
  }
}
