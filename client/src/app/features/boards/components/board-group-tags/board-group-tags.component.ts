import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatBadge } from '@angular/material/badge';
import { MatCheckbox } from '@angular/material/checkbox';
import { MatIcon } from '@angular/material/icon';
import {
  MatMenu,
  MatMenuContent,
  MatMenuTrigger,
} from '@angular/material/menu';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { Selected } from '@core/models/selected';
import { Tag } from '@core/models/tag';
import * as TagActions from '@core/store/tags/tags.actions';
import * as TagSelectors from '@core/store/tags/tags.selectors';
import { Store } from '@ngrx/store';
import { BoardGroupHeaderActionComponent } from '../board-group-header/board-group-header-action.component';
import { LucideTag } from '@lucide/angular';

@Component({
  selector: 'app-board-group-tags',
  styleUrls: ['./board-group-tags.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatMenuTrigger,
    MatBadge,
    MatIcon,
    MatMenu,
    MatMenuContent,
    MatCheckbox,
    MatProgressSpinner,
    BoardGroupHeaderActionComponent,
  ],
  template: `
    <app-board-group-header-action
      label="Filter by Tag"
      [icon]="lucideTag"
      (action)="onClicked()"
      [matMenuTriggerFor]="tagMenu"
      [matBadge]="!!selectedCount() ? selectedCount() : undefined"
      [color]="!!selectedCount() ? 'primary' : undefined" />

    <mat-menu #tagMenu="matMenu">
      <ng-template matMenuContent>
        <div class="tags-menu">
          @if (loaded()) {
            @if (tags().length) {
              @for (tag of tags(); track trackByTag($index, tag)) {
                <mat-checkbox
                  class="tag-checkbox mat-menu-item"
                  role="menuitemcheckbox"
                  (click)="$event.stopPropagation()"
                  (change)="onOptionClicked(tag)"
                  [checked]="tag.selected ?? false">
                  {{ tag.name }}
                </mat-checkbox>
              }
            } @else {
              <div class="no-tags-message">
                <mat-icon class="material-icons-outlined">
                  local_offer
                </mat-icon>
                <span> There are currently No Tags </span>
                <p>Tags Assigned to tasks will show here</p>
              </div>
            }
          } @else {
            <div class="loading">
              <mat-spinner diameter="24" />
            </div>
          }
        </div>
      </ng-template>
    </mat-menu>
  `,
})
export class BoardGroupTagsComponent {
  private store = inject(Store);

  lucideTag = LucideTag;

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
