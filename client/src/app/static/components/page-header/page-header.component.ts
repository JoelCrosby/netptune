import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
  output,
} from '@angular/core';
import { MatIcon } from '@angular/material/icon';
import {
  MatMenu,
  MatMenuContent,
  MatMenuItem,
  MatMenuTrigger,
} from '@angular/material/menu';
import { RouterLink } from '@angular/router';
import { openSideMenu } from '@core/store/layout/layout.actions';
import { selectIsMobileView } from '@core/store/layout/layout.selectors';
import { HeaderAction } from '@core/types/header-action';
import { Store } from '@ngrx/store';
import { ButtonComponent } from '../button/button.component';
import { InlineEditInputComponent } from '../inline-edit-input/inline-edit-input.component';

@Component({
  selector: 'app-page-header',
  templateUrl: './page-header.component.html',
  styleUrls: ['./page-header.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatIcon,
    RouterLink,
    InlineEditInputComponent,
    MatMenuTrigger,
    MatMenu,
    MatMenuContent,
    MatMenuItem,
    ButtonComponent,
  ],
})
export class PageHeaderComponent {
  private store = inject(Store);

  readonly title = input<string | null>();
  readonly titleEditable = input(false);
  readonly actionTitle = input<string | null>();
  readonly backLink = input<string[] | number[] | null>();
  readonly backLabel = input<string | null>();
  readonly secondaryActions = input<HeaderAction[]>([]);
  readonly overflowActions = input<HeaderAction[]>([]);

  readonly actionClick = output();
  readonly titleSubmitted = output<string>();

  readonly showSideNavToggle = this.store.selectSignal(selectIsMobileView);

  onOpenMenu() {
    this.store.dispatch(openSideMenu());
  }
}
