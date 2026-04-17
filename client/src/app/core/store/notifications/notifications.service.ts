import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { NotificationViewModel } from '@core/models/view-models/notification-view-model';

@Injectable({ providedIn: 'root' })
export class NotificationsService {
  private http = inject(HttpClient);

  getAll() {
    return this.http.get<ClientResponse<NotificationViewModel[]>>('api/notifications');
  }

  getUnreadCount() {
    return this.http.get<ClientResponse<number>>('api/notifications/unread-count');
  }

  markAsRead(id: number) {
    return this.http.put<ClientResponse>(`api/notifications/${id}/read`, null);
  }

  markAllAsRead() {
    return this.http.put<ClientResponse>('api/notifications/read-all', null);
  }
}
