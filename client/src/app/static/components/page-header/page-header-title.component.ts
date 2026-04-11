import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
} from '@angular/core';
import { InlineEditInputComponent } from '../inline-edit-input/inline-edit-input.component';

@Component({
  selector: 'app-page-header-title',
  template: `
    <div class="flex h-full flex-row items-center justify-start">
      @if (!titleEditable()) {
        <h1
          class="page-header-title font-overpass m-0 text-[2rem] font-normal max-[600px]:text-[1.4rem]">
          {{ title() }}
        </h1>
      }
      @if (titleEditable()) {
        <app-inline-edit-input
          class="page-header-title font-overpass m-0 cursor-pointer text-[2rem] font-normal"
          activeBorder="true"
          [value]="title()"
          [size]="title()?.length"
          (submitted)="titleSubmitted.emit($event)">
        </app-inline-edit-input>
      }

      <div class="ml-[1.4rem] flex flex-row items-center gap-3">
        <ng-content />
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [InlineEditInputComponent],
})
export class PageHeaderTitleComponent {
  readonly title = input<string | null>();
  readonly titleEditable = input(false);

  readonly titleSubmitted = output<string>();
}
