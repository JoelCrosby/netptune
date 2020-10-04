import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  Output,
} from '@angular/core';
import { HeaderAction } from '@core/types/header-action';

@Component({
  selector: 'app-card-list-item',
  templateUrl: './card-list-item.component.html',
  styleUrls: ['./card-list-item.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CardListItemComponent {
  @Input() title: string;
  @Input() description: string;
  @Input() subText: string;
  @Input() actions: HeaderAction[];

  @Output() delete = new EventEmitter();
}
