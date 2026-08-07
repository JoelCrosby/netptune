import { Overlay, OverlayConfig, OverlayRef } from '@angular/cdk/overlay';
import { CdkPortal } from '@angular/cdk/portal';
import {
  Component,
  computed,
  ElementRef,
  HostListener,
  inject,
  input,
  OnDestroy,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { LucideCheck, LucideChevronDown } from '@lucide/angular';

export interface InlineSelectOption {
  value: string;
  label: string;
}

let nextPanelId = 0;

@Component({
  selector: 'app-inline-select',
  imports: [CdkPortal, LucideCheck, LucideChevronDown],
  host: { class: 'block' },
  template: `
    <button
      #trigger
      type="button"
      role="combobox"
      class="hover:bg-foreground/5 focus-visible:ring-primary/50 group flex w-full cursor-pointer items-center justify-between gap-2 rounded-sm px-2 py-1.5 text-left text-sm transition-colors focus-visible:ring-2 focus-visible:outline-none"
      [class.bg-foreground/5]="isOpen()"
      [disabled]="disabled()"
      [attr.aria-expanded]="isOpen()"
      [attr.aria-label]="ariaLabel()"
      [attr.aria-controls]="panelId"
      [attr.aria-activedescendant]="activeOptionId()"
      aria-haspopup="listbox"
      (click)="toggle()"
      (keydown)="onTriggerKeydown($event)">
      <span class="truncate" [class.text-muted]="!selectedLabel()">
        {{ selectedLabel() || placeholder() }}
      </span>

      <svg
        lucideChevronDown
        class="text-muted group-hover:text-foreground h-3.5 w-3.5 shrink-0 transition-colors"
        [class.text-foreground]="isOpen()"></svg>
    </button>

    <ng-template cdkPortal>
      <div
        role="listbox"
        [id]="panelId"
        class="border-border bg-card custom-scroll max-h-64 overflow-y-auto rounded-md border p-1 shadow-lg"
        [style.width.px]="panelWidth()">
        @for (option of options(); track option.value; let index = $index) {
          <button
            type="button"
            role="option"
            [id]="panelId + '-' + index"
            class="flex w-full cursor-pointer items-center justify-between gap-2 rounded-sm px-2 py-1.5 text-left text-sm transition-colors"
            [class.bg-foreground/10]="index === activeIndex()"
            [attr.aria-selected]="option.value === value()"
            (mouseenter)="activeIndex.set(index)"
            (click)="select(option.value)">
            <span class="truncate">{{ option.label }}</span>

            @if (option.value === value()) {
              <svg lucideCheck class="text-primary h-3.5 w-3.5 shrink-0"></svg>
            }
          </button>
        }
      </div>
    </ng-template>
  `,
})
export class InlineSelectComponent implements OnDestroy {
  private readonly overlay = inject(Overlay);

  readonly options = input.required<InlineSelectOption[]>();
  readonly value = input<string>('');
  readonly placeholder = input('');
  readonly ariaLabel = input<string>();
  readonly disabled = input(false);

  readonly changed = output<string>();

  protected readonly panelId = `inline-select-${nextPanelId++}`;
  protected readonly isOpen = signal(false);
  protected readonly activeIndex = signal(0);
  protected readonly panelWidth = signal(0);

  protected readonly activeOptionId = computed(() => {
    return this.isOpen() ? `${this.panelId}-${this.activeIndex()}` : null;
  });

  private readonly portal = viewChild.required(CdkPortal);
  private readonly trigger =
    viewChild.required<ElementRef<HTMLButtonElement>>('trigger');

  private overlayRef?: OverlayRef;

  protected readonly selectedLabel = computed(() => {
    const selected = this.options().find(
      (option) => option.value === this.value()
    );

    return selected?.label ?? '';
  });

  ngOnDestroy() {
    this.overlayRef?.dispose();
  }

  @HostListener('window:resize')
  onWindowResize() {
    if (this.isOpen()) {
      this.close();
    }
  }

  protected toggle() {
    if (this.isOpen()) {
      this.close();

      return;
    }

    this.open();
  }

  protected select(value: string) {
    this.changed.emit(value);
    this.close();
    this.trigger().nativeElement.focus();
  }

  protected onTriggerKeydown(event: KeyboardEvent) {
    if (this.isOpen()) {
      this.onOpenKeydown(event);

      return;
    }

    const opensPanel =
      event.key === 'Enter' ||
      event.key === ' ' ||
      event.key === 'ArrowDown' ||
      event.key === 'ArrowUp';

    if (opensPanel) {
      event.preventDefault();
      this.open();
    }
  }

  private onOpenKeydown(event: KeyboardEvent) {
    const lastIndex = this.options().length - 1;

    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.activeIndex.update((index) => Math.min(index + 1, lastIndex));

        return;
      case 'ArrowUp':
        event.preventDefault();
        this.activeIndex.update((index) => Math.max(index - 1, 0));

        return;
      case 'Home':
        event.preventDefault();
        this.activeIndex.set(0);

        return;
      case 'End':
        event.preventDefault();
        this.activeIndex.set(lastIndex);

        return;
      case 'Enter':
      case ' ': {
        event.preventDefault();

        const active = this.options()[this.activeIndex()];

        if (active) {
          this.select(active.value);
        }

        return;
      }
      case 'Escape':
        event.preventDefault();
        this.close();

        return;
      default:
        this.focusByTypeAhead(event.key);
    }
  }

  private focusByTypeAhead(key: string) {
    const isCharacter = key.length === 1;

    if (!isCharacter) return;

    const search = key.toLowerCase();
    const index = this.options().findIndex((option) =>
      option.label.toLowerCase().startsWith(search)
    );

    if (index >= 0) {
      this.activeIndex.set(index);
    }
  }

  private open() {
    const origin = this.trigger().nativeElement;
    const selectedIndex = this.options().findIndex(
      (option) => option.value === this.value()
    );

    this.panelWidth.set(origin.getBoundingClientRect().width);
    this.activeIndex.set(selectedIndex >= 0 ? selectedIndex : 0);

    this.overlayRef = this.overlay.create(this.buildConfig(origin));
    this.overlayRef.attach(this.portal());
    this.overlayRef.backdropClick().subscribe(() => this.close());
    this.isOpen.set(true);
  }

  private close() {
    this.overlayRef?.dispose();
    this.overlayRef = undefined;
    this.isOpen.set(false);
  }

  private buildConfig(origin: HTMLElement): OverlayConfig {
    const positionStrategy = this.overlay
      .position()
      .flexibleConnectedTo(origin)
      .withPush(true)
      .withPositions([
        {
          originX: 'start',
          originY: 'bottom',
          overlayX: 'start',
          overlayY: 'top',
          offsetY: 2,
        },
        {
          originX: 'start',
          originY: 'top',
          overlayX: 'start',
          overlayY: 'bottom',
          offsetY: -2,
        },
      ]);

    return new OverlayConfig({
      positionStrategy,
      scrollStrategy: this.overlay.scrollStrategies.reposition(),
      hasBackdrop: true,
      backdropClass: 'cdk-overlay-transparent-backdrop',
    });
  }
}
