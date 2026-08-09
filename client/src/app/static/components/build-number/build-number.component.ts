import { Component, inject } from '@angular/core';
import { BuildInfoService } from '@core/services/build-info.service';

@Component({
  selector: 'app-build-number',
  template: `
    @if (buildInfo(); as buildInfo) {
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
        <span>|</span>
        <span class="buildNumber">
          <ng-container
            i18n="
              Build identifier in the footer. NUMBER is the CI build number
            ">
            BUILD
            {{
              buildInfo.buildNumber // i18n(ph="NUMBER")
            }}
          </ng-container>
        </span>
        <span>|</span>
        <a
          class="runId"
          [href]="
            'https://github.com/JoelCrosby/Netptune/actions/runs/' +
            buildInfo.runId
          "
          target="_blank"
          rel="noopener noreferrer">
          <span i18n="Footer link to the CI run that produced this build">
            Github Action
          </span>
        </a>
      </div>
    }
  `,
})
export class BuildNumberComponent {
  readonly buildInfo = inject(BuildInfoService).buildInfo;
}
