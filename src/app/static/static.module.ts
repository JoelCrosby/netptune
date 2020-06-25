import { SharedModule } from '@app/shared/shared.module';
// Angular modules
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PageContainerComponent } from './components/page-container/page-container.component';
import { UsernamePipe } from './pipes/username.pipe';
import { AvatarComponent } from './components/avatar/avatar.component';
import { AvatarPipe } from './pipes/avatar.pipe';
import { SpinnerComponent } from './components/spinner/spinner.component';
import { PageHeaderComponent } from './components/page-header/page-header.component';
import { InlineEditInputComponent } from './components/inline-edit-input/inline-edit-input.component';

@NgModule({
  declarations: [
    PageContainerComponent,
    UsernamePipe,
    AvatarComponent,
    AvatarPipe,
    SpinnerComponent,
    PageHeaderComponent,
    InlineEditInputComponent,
  ],
  imports: [CommonModule, SharedModule],
  exports: [
    PageContainerComponent,
    UsernamePipe,
    AvatarComponent,
    AvatarPipe,
    SpinnerComponent,
    PageHeaderComponent,
    InlineEditInputComponent,
  ],
})
export class StaticModule {}
