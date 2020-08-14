import {
  Component,
  OnInit,
  ChangeDetectionStrategy,
  Input,
} from '@angular/core';
import { getColourForKey } from '@core/util/colors/color-util';

@Component({
  selector: 'app-avatar',
  templateUrl: './avatar.component.html',
  styleUrls: ['./avatar.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AvatarComponent implements OnInit {
  @Input() name: string;
  @Input() size: string;

  backgroundColor: string;
  color = '#fff';

  constructor() {}

  ngOnInit() {
    this.backgroundColor = getColourForKey(this.name);
  }
}
