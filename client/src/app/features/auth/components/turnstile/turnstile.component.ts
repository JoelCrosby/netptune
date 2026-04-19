import { HttpClient } from '@angular/common/http';
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  inject,
  OnDestroy,
  output,
} from '@angular/core';
import { selectEffectiveTheme } from '@app/core/store/settings/settings.selectors';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-turnstile',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<div id="turnstile-container"></div>`,
})
export class TurnstileComponent implements AfterViewInit, OnDestroy {
  store = inject(Store);
  http = inject(HttpClient);
  turnstileElId?: string;

  tokenGenerated = output<string>();

  ngAfterViewInit() {
    const effectiveTheme = this.store.selectSignal(selectEffectiveTheme);
    const theme = effectiveTheme() === 'dark' ? 'dark' : 'light';

    this.turnstileElId = window.turnstile.render('#turnstile-container', {
      sitekey: '1x00000000000000000000AA',
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
