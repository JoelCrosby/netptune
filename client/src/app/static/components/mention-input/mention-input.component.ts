import { CdkPortal } from '@angular/cdk/portal';
import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
  input,
  OnDestroy,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { AppUser } from '@core/models/appuser';
import { LucideDynamicIcon, LucideIconInput } from '@lucide/angular';
import { AbstractFormValueControl } from '../abstract-form-value-control';
import { AvatarComponent } from '../avatar/avatar.component';
import { FormErrorComponent } from '../form-error/form-error.component';

export interface MentionSubmitEvent {
  text: string;
  mentions: string[];
}

@Component({
  selector: 'app-mention-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AvatarComponent, CdkPortal, LucideDynamicIcon, FormErrorComponent],
  template: `
    <div class="nept-form-control mb-0!">
      @if (label()) {
        <label [for]="name()" class="form-control-label">
          {{ label() }}
        </label>
      }

      <div
        class="form-control-input"
        [class.invalid]="touched() && invalid()"
        [class.active]="pending()">
        @if (prefix()) {
          <div class="form-control-prefix">{{ prefix() }}</div>
        }

        <input
          #inputEl
          [id]="name()"
          [value]="value()"
          [disabled]="disabled()"
          [class.form-control-disabled]="disabled()"
          [attr.autocomplete]="autocomplete()"
          [attr.placeholder]="placeholder()"
          [style.padding]="prefix() ? '0 .8rem 0 0' : '0 .8rem'"
          (input)="onInput($event)"
          (keydown)="onKeyDown($event)"
          (blur)="onBlur()" />

        @if (icon()) {
          <svg
            class="mr-3"
            [lucideIcon]="icon()!"
            size="20"
            aria-hidden="true"></svg>
        }
      </div>

      @if (hint()) {
        <small class="form-control-hint"> {{ hint() }} </small>
      }

      @if (errors()) {
        @for (error of errors(); track error.kind) {
          <app-form-error>
            {{ error.message }}
          </app-form-error>
        }
      }

      <div class="form-control-content">
        <ng-content />
      </div>
    </div>

    <ng-template cdkPortal>
      <div
        class="bg-card border-border z-50 flex max-h-48 w-56 flex-col overflow-y-auto rounded border shadow-lg">
        @for (user of filteredUsers(); track user.id) {
          <button
            class="hover:bg-hover flex cursor-pointer flex-row items-center gap-2 border-0 bg-transparent px-3 py-2 text-left text-sm"
            [class.bg-hover]="activeIndex() === $index"
            (mousedown)="selectUser(user)">
            <app-avatar
              size="sm"
              [name]="user.displayName"
              [imageUrl]="user.pictureUrl" />
            <span class="truncate">{{ user.displayName }}</span>
          </button>
        } @empty {
          <div class="text-muted-foreground px-3 py-2 text-sm">No results</div>
        }
      </div>
    </ng-template>
  `,
})
export class MentionInputComponent
  extends AbstractFormValueControl
  implements OnDestroy
{
  private overlay = inject(Overlay);
  private readonly portal = viewChild.required(CdkPortal);
  readonly inputEl =
    viewChild.required<ElementRef<HTMLInputElement>>('inputEl');

  readonly label = input<string>();
  readonly icon = input<LucideIconInput | null>();
  readonly prefix = input<string | null>();
  readonly autocomplete = input('off');
  readonly placeholder = input<string | null>(
    'Add comment — type @ to mention'
  );
  readonly hint = input<string | null>();
  readonly loading = input<boolean | null>(false);
  readonly pending = input(false);

  readonly users = input<AppUser[] | null>([]);

  readonly mentionSubmit = output<MentionSubmitEvent>();

  filteredUsers = signal<AppUser[]>([]);
  activeIndex = signal(0);

  private overlayRef?: OverlayRef;
  private mentionStart = -1;
  private mentionedUserIds = new Set<string>();

  onInput(event: Event) {
    const el = event.target as HTMLInputElement;
    const val = el.value;
    const cursor = el.selectionStart ?? val.length;

    this.value.set(val);

    const atIndex = this.findActiveMentionStart(val, cursor);

    if (atIndex >= 0) {
      const query = val.slice(atIndex + 1, cursor);
      this.mentionStart = atIndex;
      this.openDropdown(query, el);
    } else {
      this.mentionStart = -1;
      this.closeDropdown();
    }
  }

  onKeyDown(event: KeyboardEvent) {
    if (!this.overlayRef?.hasAttached()) {
      if (event.key === 'Enter') {
        event.preventDefault();
        this.submit();
      }
      return;
    }

    const users = this.filteredUsers();

    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.activeIndex.update((i) => Math.min(i + 1, users.length - 1));
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.activeIndex.update((i) => Math.max(i - 1, 0));
        break;
      case 'Enter':
      case 'Tab':
        event.preventDefault();
        if (users[this.activeIndex()]) {
          this.selectUser(users[this.activeIndex()]);
        }
        break;
      case 'Escape':
        this.closeDropdown();
        break;
    }
  }

  onBlur() {
    this.touched.set(true);
    setTimeout(() => this.closeDropdown(), 150);
  }

  selectUser(user: AppUser) {
    const val = this.value();
    const el = this.inputEl().nativeElement;
    const cursor = el.selectionStart ?? val.length;
    const before = val.slice(0, this.mentionStart);
    const after = val.slice(cursor);
    const inserted = `@${user.displayName} `;

    const newValue = before + inserted + after;
    this.value.set(newValue);
    this.mentionedUserIds.add(user.id);
    this.mentionStart = -1;
    this.closeDropdown();

    el.value = newValue;
    const newCursor = before.length + inserted.length;
    el.setSelectionRange(newCursor, newCursor);
    el.focus();
  }

  submit() {
    const text = this.value().trim();
    if (!text) return;

    this.mentionSubmit.emit({ text, mentions: [...this.mentionedUserIds] });
    this.value.set('');
    this.mentionedUserIds.clear();
    this.inputEl().nativeElement.value = '';
  }

  private findActiveMentionStart(value: string, cursor: number): number {
    for (let i = cursor - 1; i >= 0; i--) {
      if (value[i] === '@') {
        const before = value[i - 1];
        if (i === 0 || before === ' ' || before === '\n') return i;
        return -1;
      }
      if (value[i] === ' ') return -1;
    }
    return -1;
  }

  private openDropdown(query: string, origin: HTMLInputElement) {
    const allUsers = this.users() ?? [];
    const lower = query.toLowerCase();
    const filtered = lower
      ? allUsers.filter((u) => u.displayName.toLowerCase().includes(lower))
      : allUsers;

    this.filteredUsers.set(filtered);
    this.activeIndex.set(0);

    if (!this.overlayRef?.hasAttached()) {
      this.overlayRef = this.overlay.create({
        positionStrategy: this.overlay
          .position()
          .flexibleConnectedTo(origin)
          .withPush(false)
          .withPositions([
            {
              originX: 'start',
              originY: 'top',
              overlayX: 'start',
              overlayY: 'bottom',
              offsetY: -4,
            },
            {
              originX: 'start',
              originY: 'bottom',
              overlayX: 'start',
              overlayY: 'top',
              offsetY: 4,
            },
          ]),
        width: '224px',
        hasBackdrop: false,
        scrollStrategy: this.overlay.scrollStrategies.reposition(),
      });

      this.overlayRef.attach(this.portal());
    }
  }

  private closeDropdown() {
    this.overlayRef?.dispose();
    this.overlayRef = undefined;
    this.filteredUsers.set([]);
  }

  ngOnDestroy() {
    this.overlayRef?.dispose();
  }
}
