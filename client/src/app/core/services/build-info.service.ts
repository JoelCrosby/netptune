import { httpResource } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BuildInfo } from '@core/models/build-info';

@Injectable({ providedIn: 'root' })
export class BuildInfoService {
  private readonly resource = httpResource<BuildInfo>(() => ({
    url: 'api/meta/build-info',
  }));

  readonly buildInfo = this.resource.value;
}
