import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatBadge } from '@angular/material/badge';
import { MatButton } from '@angular/material/button';
import { MatCheckbox } from '@angular/material/checkbox';
import { MatIcon } from '@angular/material/icon';
import {
  MatMenu,
  MatMenuContent,
  MatMenuTrigger,
} from '@angular/material/menu';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatTooltip } from '@angular/material/tooltip';
import { Selected } from '@core/models/selected';
import { Tag } from '@core/models/tag';
import * as TagActions from '@core/store/tags/tags.actions';
import * as TagSelectors from '@core/store/tags/tags.selectors';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-board-group-tags',
  templateUrl: './board-group-tags.component.html',
  styleUrls: ['./board-group-tags.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatButton,
    MatTooltip,
    MatMenuTrigger,
    MatBadge,
    MatIcon,
    MatMenu,
    MatMenuContent,
    MatCheckbox,
    MatProgressSpinner,
  ],
})
export class BoardGroupTagsComponent {
  private store = inject(Store);

  tags = this.store.selectSignal(TagSelectors.selectTasksWithSelect);
  loaded = this.store.selectSignal(TagSelectors.selectTagsLoaded);
  selectedCount = this.store.selectSignal(TagSelectors.selectSelectedTagCount);

  trackByTag(_: number, tag: Selected<Tag>) {
    return tag.id;
  }

  onClicked() {
    this.store.dispatch(TagActions.loadTags());
  }

  onOptionClicked(selected: Selected<Tag>) {
    const tag = selected.name;
    this.store.dispatch(TagActions.toggleSelectedTag({ tag }));
  }
}
