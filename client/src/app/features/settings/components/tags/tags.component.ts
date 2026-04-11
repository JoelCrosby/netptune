import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  effect,
  ElementRef,
  inject,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { Tag } from '@core/models/tag';
import * as actions from '@core/store/tags/tags.actions';
import { selectTags } from '@core/store/tags/tags.selectors';
import { LucideX } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { DocumentService } from '@static/services/document.service';
import { TagsInputComponent } from '../tags-input/tags-input.component';

@Component({
  selector: 'app-tags',
  templateUrl: './tags.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TagsInputComponent, TooltipDirective, LucideX],
})
export class TagsComponent {
  private store = inject(Store);
  private cd = inject(ChangeDetectorRef);
  readonly document = inject(DocumentService);
  readonly input = viewChild<ElementRef>('tagsinput');

  tags = this.store.selectSignal(selectTags);

  addTagActive = signal(false);
  editTagIndex = signal<number | null>(null);

  constructor() {
    this.store.dispatch(actions.loadTags());

    effect(() => {
      const el = this.document.documentClicked();
      untracked(() => this.handleDocumentClick(el));
    });
  }

  onItemClicked(index: number) {
    this.editTagIndex.set(index);
    this.addTagActive.set(false);
  }

  onEditTagSubmit(value: string, tag: Tag) {
    if (!value) return;

    const currentValue = tag.name;
    const newValue = value;

    this.store.dispatch(actions.editTag({ currentValue, newValue }));
  }

  onEditCanceled() {
    this.editTagIndex.set(null);
    this.cd.detectChanges();
  }

  onAddTagClicked() {
    this.editTagIndex.set(null);
    this.addTagActive.set(true);
  }

  onAddTagSubmit(name: string) {
    this.store.dispatch(actions.addTag({ name }));
  }

  onAddCanceled() {
    this.addTagActive.set(false);
    this.cd.detectChanges();
  }

  onDeleteClicked(tag: Tag) {
    if (!tag) return;

    const tags = [tag.name];
    this.store.dispatch(actions.deleteTags({ tags }));
  }

  handleDocumentClick(target: EventTarget) {
    if (!this.addTagActive()) return;

    const input = this.input();

    if (!input?.nativeElement) return;

    if (!input.nativeElement.contains(target)) {
      this.onAddCanceled();
    }
  }
}
