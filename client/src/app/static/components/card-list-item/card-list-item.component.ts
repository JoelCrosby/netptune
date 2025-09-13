import {
  ChangeDetectionStrategy,
  Component,
  input,
  output
} from '@angular/core';
import { HeaderAction } from '@core/types/header-action';
import { CardComponent } from '../card/card.component';
import { CardTitleComponent } from '../card/card-title.component';
import { MatButton, MatAnchor } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { CardContentComponent } from '../card/card-content.component';
import { CardActionsComponent } from '../card/card-actions.component';
import { NgFor, NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-card-list-item',
  templateUrl: './card-list-item.component.html',
  styleUrls: ['./card-list-item.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CardComponent,
    CardTitleComponent,
    MatButton,
    MatIcon,
    CardContentComponent,
    CardActionsComponent,
    NgFor,
    NgIf,
    MatAnchor,
    RouterLink,
  ],
})
export class CardListItemComponent {
  readonly title = input.required<string>();
  readonly description = input.required<string>();
  readonly subText = input<string>();
  readonly subTextLabel = input<string>();
  readonly actions = input.required<HeaderAction[] | null>();

  readonly delete = output();
}
