import { SharedModule } from '@app/shared/shared.module';
// Angular modules
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PageContainerComponent } from './components/page-container/page-container.component';
import { UsernamePipe } from './pipes/username.pipe';
import { AvatarComponent } from './components/avatar/avatar.component';
import { AvatarPipe } from './pipes/avatar.pipe';
import { SpinnerComponent } from './components/spinner/spinner.component';

@NgModule({
  declarations: [
    PageContainerComponent,
    UsernamePipe,
    AvatarComponent,
    AvatarPipe,
    SpinnerComponent,
  ],
  imports: [CommonModule, SharedModule],
  exports: [
    PageContainerComponent,
    UsernamePipe,
    AvatarComponent,
    AvatarPipe,
    SpinnerComponent,
  ],
})
export class StaticModule {}
