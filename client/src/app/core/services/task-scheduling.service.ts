import { HttpClient } from '@angular/common/http';
import { Service, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { TaskSchedule } from '@core/models/scheduled-task';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { unwrapClientResponse } from '@core/util/rxjs-operators';
import { map, Observable } from 'rxjs';

@Service()
export class TaskSchedulingService {
  private readonly http = inject(HttpClient);

  updateSchedule(taskId: number, schedule: TaskSchedule): Observable<void> {
    return this.http
      .put<ClientResponse<TaskViewModel>>('api/tasks', {
        id: taskId,
        startDate: schedule.startDate,
        dueDate: schedule.endDate,
      })
      .pipe(
        unwrapClientResponse(),
        map(() => undefined)
      );
  }
}
