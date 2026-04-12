import {
  CdkFixedSizeVirtualScroll,
  CdkVirtualForOf,
  CdkVirtualScrollViewport,
} from '@angular/cdk/scrolling';
import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ContentChild,
  TemplateRef,
  TrackByFunction,
  input,
} from '@angular/core';

@Component({
  selector: 'app-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CdkVirtualScrollViewport,
    CdkFixedSizeVirtualScroll,
    CdkVirtualForOf,
    NgTemplateOutlet,
  ],
  template: `
    <div class="w-full">
      @if (header()) {
        <h4
          class="text-foreground/60 mb-2 text-sm font-normal tracking-[.25px]">
          {{ header() }}
        </h4>
      }
      <div
        class="bg-board-group mb-5 flex h-full min-h-49 flex-1 flex-col overflow-hidden rounded p-2.5">
        @if (items() !== undefined) {
          @if (items()?.length) {
            <cdk-virtual-scroll-viewport
              [class]="viewportClass() + ' custom-scroll'"
              [itemSize]="itemSize()"
              minBufferPx="1024"
              maxBufferPx="2048">
              <ng-template
                cdkVirtualFor
                [cdkVirtualForOf]="items()"
                [cdkVirtualForTemplate]="$any(itemTemplate)"
                [cdkVirtualForTrackBy]="trackBy() ?? defaultTrackBy"
                [cdkVirtualForTemplateCacheSize]="0"></ng-template>
              @if (footerTemplate) {
                <ng-container *ngTemplateOutlet="footerTemplate" />
              }
            </cdk-virtual-scroll-viewport>
          } @else {
            @if (emptyTemplate) {
              <ng-container *ngTemplateOutlet="emptyTemplate" />
            } @else {
              <div
                class="flex flex-1 flex-col items-center justify-center opacity-60">
                <p class="text-center text-sm">
                  {{ emptyMessage() || 'No items to display' }}
                </p>
              </div>
            }
          }
        } @else {
          <ng-content />
        }
      </div>
    </div>
  `,
})
export class ListComponent {
  readonly header = input<string>();
  readonly items = input<unknown[] | null>();
  readonly itemSize = input(40);
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  readonly trackBy = input<TrackByFunction<any>>();
  readonly viewportClass = input('h-[calc(100vh-200px)] min-h-16');
  readonly emptyMessage = input<string>();

  @ContentChild('item') itemTemplate?: TemplateRef<unknown>;
  @ContentChild('listFooter') footerTemplate?: TemplateRef<void>;
  @ContentChild('listEmpty') emptyTemplate?: TemplateRef<void>;

  readonly defaultTrackBy: TrackByFunction<unknown> = (_, item) => item;
}
