import {
  Component,
  computed,
  ElementRef,
  input,
  viewChildren,
} from '@angular/core';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import {
  ColorDefinition,
  colorDictionary,
  resolveColorName,
} from '@core/util/colors/colors';
import { LucideCheck } from '@lucide/angular';
import { AbstractFormValueControl } from '../abstract-form-value-control';
import {
  FormControlHintDirective,
  FormControlLabelDirective,
} from '../form-control/form-control.directives';
import { FormErrorComponent } from '../form-error/form-error.component';
import { labelIdFor } from '../form-control-a11y';

@Component({
  selector: 'app-color-select',
  imports: [
    TooltipDirective,
    LucideCheck,
    FormControlLabelDirective,
    FormControlHintDirective,
    FormErrorComponent,
  ],
  template: `
    <div class="nept-form-control mb-[1.4rem] w-[inherit]">
      @if (label()) {
        <span [id]="labelId()" appFormLabel>
          {{ label() }}
        </span>
      }

      <div
        role="radiogroup"
        [attr.aria-labelledby]="label() ? labelId() : null"
        [attr.aria-describedby]="describedBy(!!hint())"
        [attr.aria-invalid]="ariaInvalid()">
        @for (row of rows(); track $index) {
          <div class="flex justify-stretch">
            @for (color of row; track color.name) {
              <button
                #swatch
                type="button"
                role="radio"
                class="focus-visible:ring-primary m-[0.2rem] flex h-9 min-h-9 min-w-9 flex-1 cursor-pointer items-center justify-center rounded-sm text-white focus-visible:ring-2 focus-visible:outline-none"
                [appTooltip]="color.label"
                [class]="color.swatchClass"
                [attr.aria-label]="color.label"
                [attr.aria-checked]="isSelected(color)"
                [attr.tabindex]="isTabbable(color) ? 0 : -1"
                [disabled]="disabled()"
                (click)="onOptionClicked(color)"
                (keydown)="onKeydown($event, color)">
                @if (isSelected(color)) {
                  <svg lucideCheck class="h-6 w-6"></svg>
                }
              </button>
            }
          </div>
        }
      </div>

      @if (hint()) {
        <small [id]="hintId()" appFormHint>{{ hint() }}</small>
      }

      @if (showErrors()) {
        <div [id]="errorId()">
          @for (error of errors(); track error.kind) {
            <app-form-error>
              {{ error.message }}
            </app-form-error>
          }
        </div>
      }
    </div>
  `,
})
export class ColorSelectComponent extends AbstractFormValueControl {
  readonly label = input.required<string>();
  readonly hint = input<string | null>(null);

  readonly colors = colorDictionary();
  readonly labelId = computed(() => labelIdFor(this.name()));

  private readonly swatches =
    viewChildren<ElementRef<HTMLButtonElement>>('swatch');
  private readonly rowLength = Math.ceil(this.colors.length / 2);

  readonly rows = computed(() => [
    this.colors.slice(0, this.rowLength),
    this.colors.slice(this.rowLength),
  ]);

  private readonly selectedIndex = computed(() => {
    const selectedName = resolveColorName(this.value());

    return this.colors.findIndex((color) => color.name === selectedName);
  });

  isSelected(color: ColorDefinition): boolean {
    return resolveColorName(this.value()) === color.name;
  }

  isTabbable(color: ColorDefinition): boolean {
    const selectedIndex = this.selectedIndex();
    const index = this.colors.indexOf(color);

    return selectedIndex === -1 ? index === 0 : selectedIndex === index;
  }

  onOptionClicked(color: ColorDefinition) {
    this.value.set(color.name);
    this.touched.set(true);
  }

  onKeydown(event: KeyboardEvent, color: ColorDefinition) {
    const nextIndex = this.resolveNextIndex(
      event.key,
      this.colors.indexOf(color)
    );

    if (nextIndex === null) return;

    event.preventDefault();

    const nextColor = this.colors[nextIndex];

    this.onOptionClicked(nextColor);
    this.swatches()[nextIndex]?.nativeElement.focus();
  }

  private resolveNextIndex(key: string, currentIndex: number): number | null {
    const lastIndex = this.colors.length - 1;

    switch (key) {
      case 'ArrowRight':
      case 'ArrowDown':
        return currentIndex === lastIndex ? 0 : currentIndex + 1;
      case 'ArrowLeft':
      case 'ArrowUp':
        return currentIndex === 0 ? lastIndex : currentIndex - 1;
      case 'Home':
        return 0;
      case 'End':
        return lastIndex;
      default:
        return null;
    }
  }
}
