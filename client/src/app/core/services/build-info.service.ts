import { httpResource } from '@angular/common/http';
import { Service } from '@angular/core';
import { BuildInfo } from '@core/models/build-info';

@Service()
export class BuildInfoService {
  private readonly resource = httpResource<BuildInfo>(() => ({
    url: 'api/meta/build-info',
  }));

  readonly buildInfo = this.resource.value;
}
