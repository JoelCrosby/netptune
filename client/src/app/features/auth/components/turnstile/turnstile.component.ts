import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
  OnDestroy,
  output,
  signal,
  ViewChild,
} from '@angular/core';
import { environment } from '@env/environment';
import { selectEffectiveTheme } from '@app/core/store/settings/settings.selectors';
import { Store } from '@ngrx/store';
import { TurnstileLoaderService } from './turnstile-loader.service';

@Component({
  selector: 'app-turnstile',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col gap-2">
      <div #turnstileContainer></div>

      @if (loadError()) {
        <p
          class="text-warn bg-warn/5 rounded px-3 py-2 text-sm leading-5"
          role="alert">
          We could not load the security check. Allow challenges.cloudflare.com
          in your content blocker, then refresh.
        </p>
      }
    </div>
  `,
})
export class TurnstileComponent implements AfterViewInit, OnDestroy {
  private store = inject(Store);
  private turnstileLoader = inject(TurnstileLoaderService);
  private destroyed = false;
  private effectiveTheme = this.store.selectSignal(selectEffectiveTheme);
  private turnstileElId?: string;

  @ViewChild('turnstileContainer')
  private turnstileContainer?: ElementRef<HTMLDivElement>;

  loadError = signal(false);

  tokenGenerated = output<string>();

  async ngAfterViewInit() {
    try {
      const turnstile = await this.turnstileLoader.load();

      if (this.destroyed || !this.turnstileContainer) return;

      const theme = this.effectiveTheme() === 'dark' ? 'dark' : 'light';

      this.turnstileElId = turnstile.render(
        this.turnstileContainer.nativeElement,
        {
          sitekey: environment.turnstileSitekey,
          size: 'flexible',
          theme,
          appearance: 'interaction-only',
          callback: (token: string) => {
            this.loadError.set(false);
            this.tokenGenerated.emit(token);
          },
          'error-callback': () => {
            this.loadError.set(true);
            this.tokenGenerated.emit('');
          },
          'expired-callback': () => {
            this.tokenGenerated.emit('');
          },
          'timeout-callback': () => {
            this.tokenGenerated.emit('');
          },
          'unsupported-callback': () => {
            this.loadError.set(true);
            this.tokenGenerated.emit('');
          },
        }
      );
    } catch {
      this.loadError.set(true);
      this.tokenGenerated.emit('');
    }
  }

  ngOnDestroy(): void {
    this.destroyed = true;

    if (this.turnstileElId && window.turnstile) {
      window.turnstile.remove(this.turnstileElId);
    }
  }
}
