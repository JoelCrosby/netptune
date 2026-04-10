import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-board-group-header-seperator',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<div class="border-border mx-1 h-5 border"></div>`,
})
export class BoardGroupHeaderSeperatorComponent {}
