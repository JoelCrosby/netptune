import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
} from '@angular/core';
import { loadBuildInfo } from '@core/store/meta/meta.actions';
import { selectBuildInfo } from '@core/store/meta/meta.selectors';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-build-number',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `@if (buildInfo(); as buildInfo) {
    <div
      class="fixed right-8 bottom-4 text-xs font-medium tracking-[0.125px] opacity-60">
      <a
        class="gitHashShort"
        [href]="
          'https://github.com/JoelCrosby/Netptune/commit/' + buildInfo.gitHash
        "
        target="_blank"
        rel="noopener noreferrer">
        {{ buildInfo.gitHashShort }}
      </a>
      <span> | </span>
      <span class="buildNumber"> BUILD {{ buildInfo.buildNumber }} </span>
      <span> | </span>
      <a
        class="runId"
        [href]="
          'https://github.com/JoelCrosby/Netptune/actions/runs/' +
          buildInfo.runId
        "
        target="_blank"
        rel="noopener noreferrer">
        Github Action
      </a>
    </div>
  }`,
})
export class BuildNumberComponent implements OnInit {
  private store = inject(Store);

  buildInfo = this.store.selectSignal(selectBuildInfo);

  ngOnInit() {
    this.store.dispatch(loadBuildInfo());
  }
}
