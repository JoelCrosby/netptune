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
import { StrokedButtonComponent } from '@app/static/components/button/stroked-button.component';

@Component({
  selector: 'app-tags',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    TagsInputComponent,
    TooltipDirective,
    LucideX,
    StrokedButtonComponent,
  ],
  template: `<h3 class="font-overpass mb-4 text-[1.4rem] font-normal">Tags</h3>

    <div
      class="flex max-w-[calc(300px-.4rem)] flex-col gap-2 text-base leading-5 tracking-[.1px]">
      @for (tag of tags(); track tag.id; let i = $index) {
        @if (editTagIndex() === i) {
          <app-tags-input
            [value]="tag.name"
            (submitted)="onEditTagSubmit($event, tag)"
            (canceled)="onEditCanceled()">
          </app-tags-input>
        } @else {
          <div
            role="button"
            [tabindex]="i"
            class="group flex cursor-pointer flex-row items-center rounded-sm bg-[rgba(var(--foreground-rgb),.06)] px-[calc(.6rem+2px)] py-[calc(.4rem+2px)] text-[var(--foreground)] transition-colors duration-[240ms] ease-in-out hover:bg-[rgba(var(--foreground-rgb),.1)]"
            (click)="onItemClicked(i)">
            <div class="w-full">{{ tag.name }}</div>
            <div
              role="button"
              aria-disabled="true"
              class="hidden h-5 w-5 group-hover:block"
              appTooltip="Delete Tag"
              (click)="$event.stopPropagation(); onDeleteClicked(tag)">
              <svg lucideX class="h-5 w-5"></svg>
            </div>
          </div>
        }
      }

      <div #tagsinput>
        @if (addTagActive()) {
          <app-tags-input
            (submitted)="onAddTagSubmit($event)"
            (canceled)="onAddCanceled()">
          </app-tags-input>
        } @else {
          <button app-stroked-button class="w-full" (click)="onAddTagClicked()">
            <span class="block text-sm font-medium"> Create Tag </span>
          </button>
        }
      </div>
    </div> `,
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
