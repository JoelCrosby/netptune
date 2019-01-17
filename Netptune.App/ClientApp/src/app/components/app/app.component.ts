import { Component } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { insertRemoveSidebar } from '../../animations';
import { TransitionService } from '../../services/transition/transition.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
  animations: [
    insertRemoveSidebar
  ]
})
export class AppComponent {

  subscriptions = new Subscription();

  showSidebar = true;

  constructor(
    public transitionService: TransitionService,
    private router: Router,
    private activatedRoute: ActivatedRoute) {

    this.subscriptions.add(
      this.activatedRoute.url.subscribe(() => {
        const url = this.router.url;
        const firstSlashIndex = url.indexOf('/', 1);
        const path = url.substring(1, firstSlashIndex);
        console.log(path);

      })
    );

  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

}
