import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  inject,
  OnDestroy,
  output,
} from '@angular/core';
import { environment } from '@env/environment';
import { selectEffectiveTheme } from '@app/core/store/settings/settings.selectors';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-turnstile',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<div id="turnstile-container"></div>`,
})
export class TurnstileComponent implements AfterViewInit, OnDestroy {
  private store = inject(Store);
  turnstileElId?: string;

  tokenGenerated = output<string>();

  ngAfterViewInit() {
    const effectiveTheme = this.store.selectSignal(selectEffectiveTheme);
    const theme = effectiveTheme() === 'dark' ? 'dark' : 'light';

    this.turnstileElId = window.turnstile.render('#turnstile-container', {
      sitekey: environment.turnstileSitekey,
      size: 'flexible',
      theme,
      appearance: 'interaction-only',
      callback: async (token: string) => {
        this.tokenGenerated.emit(token);
      },
    });
  }

  ngOnDestroy(): void {
    if (this.turnstileElId) {
      window.turnstile.remove(this.turnstileElId);
    }
  }
}
