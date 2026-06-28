import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { Page } from '@core/models/pagination';
import { NotificationViewModel } from '@core/models/view-models/notification-view-model';

const RECENT_NOTIFICATIONS_PAGE_SIZE = 10;

@Injectable({ providedIn: 'root' })
export class NotificationsService {
  private http = inject(HttpClient);

  getRecent() {
    return this.http.get<ClientResponse<Page<NotificationViewModel>>>(
      'api/notifications',
      { params: { page: 1, pageSize: RECENT_NOTIFICATIONS_PAGE_SIZE } }
    );
  }

  getUnreadCount() {
    return this.http.get<ClientResponse<number>>(
      'api/notifications/unread-count'
    );
  }

  markAsRead(id: number) {
    return this.http.put<ClientResponse>(`api/notifications/${id}/read`, null);
  }

  markAllAsRead() {
    return this.http.put<ClientResponse>('api/notifications/read-all', null);
  }
}
