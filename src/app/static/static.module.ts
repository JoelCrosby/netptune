import { SharedModule } from '@app/shared/shared.module';
// Angular modules
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PageContainerComponent } from './components/page-container/page-container.component';
import { UsernamePipe } from './pipes/username.pipe';
import { AvatarComponent } from './components/avatar/avatar.component';
import { AvatarPipe } from './pipes/avatar.pipe';

@NgModule({
  declarations: [
    PageContainerComponent,
    UsernamePipe,
    AvatarComponent,
    AvatarPipe,
  ],
  imports: [CommonModule, SharedModule],
  exports: [PageContainerComponent, UsernamePipe, AvatarComponent, AvatarPipe],
})
export class StaticModule {}
