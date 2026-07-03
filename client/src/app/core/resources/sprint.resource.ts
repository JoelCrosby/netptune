import { httpResource } from '@angular/common/http';
import { SprintViewModel } from '../models/view-models/sprint-view-model';
import { SprintStatus } from '../enums/sprint-status';

export const sprintResource = () => {
  return httpResource<SprintViewModel[]>(
    () => ({
      url: 'api/sprints',
      params: {
        statuses: [SprintStatus.planning, SprintStatus.active],
        take: 100,
      },
    }),
    { defaultValue: [] }
  );
};
