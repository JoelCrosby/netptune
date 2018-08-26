import { Component, OnInit } from '@angular/core';
import { dropIn } from '../../animations';

@Component({
  selector: 'app-users',
  templateUrl: './users.component.html',
  styleUrls: ['./users.component.scss'],
  animations: [dropIn]
})
export class UsersComponent implements OnInit {

  constructor() { }

  ngOnInit() {
  }

}
