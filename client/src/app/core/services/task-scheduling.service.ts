import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { TaskSchedule } from '@core/models/scheduled-task';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { map, Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
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
        unwrapClientReposne(),
        map(() => undefined)
      );
  }
}
