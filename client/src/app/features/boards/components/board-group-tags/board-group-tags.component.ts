import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { Selected } from '@core/models/selected';
import { Tag } from '@core/models/tag';
import * as TagActions from '@core/store/tags/tags.actions';
import * as TagSelectors from '@core/store/tags/tags.selectors';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { LetDirective } from '@ngrx/component';
import { MatButton } from '@angular/material/button';
import { MatTooltip } from '@angular/material/tooltip';
import { MatMenuTrigger, MatMenu, MatMenuContent } from '@angular/material/menu';
import { MatBadge } from '@angular/material/badge';
import { MatIcon } from '@angular/material/icon';
import { NgIf, NgFor, AsyncPipe } from '@angular/common';
import { MatCheckbox } from '@angular/material/checkbox';
import { MatProgressSpinner } from '@angular/material/progress-spinner';

@Component({
    selector: 'app-board-group-tags',
    templateUrl: './board-group-tags.component.html',
    styleUrls: ['./board-group-tags.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [LetDirective, MatButton, MatTooltip, MatMenuTrigger, MatBadge, MatIcon, MatMenu, MatMenuContent, NgIf, NgFor, MatCheckbox, MatProgressSpinner, AsyncPipe]
})
export class BoardGroupTagsComponent implements OnInit {
  private store = inject(Store);

  tags$!: Observable<Selected<Tag>[]>;
  loaded$!: Observable<boolean>;
  selectedCount$!: Observable<number>;

  ngOnInit() {
    this.tags$ = this.store.select(TagSelectors.selectTasksWithSelect);
    this.loaded$ = this.store.select(TagSelectors.selectTagsLoaded);
    this.selectedCount$ = this.store.select(
      TagSelectors.selectSelectedTagCount
    );
  }

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
