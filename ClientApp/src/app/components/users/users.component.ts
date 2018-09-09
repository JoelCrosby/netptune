import { Component, OnInit } from '@angular/core';
import { dropIn } from '../../animations';
import { AppUser } from '../../models/appuser';
import { UserService } from '../../services/user/user.service';

@Component({
  selector: 'app-users',
  templateUrl: './users.component.html',
  styleUrls: ['./users.component.scss'],
  animations: [dropIn]
})
export class UsersComponent implements OnInit {

  constructor(public userService: UserService) { }

  ngOnInit() {
    this.userService.refreshUsers();
  }

  trackById(index: number, user: AppUser) {
    return user.id;
  }

  showInviteModal(): void {

  }

}
