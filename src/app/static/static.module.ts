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
import { FromNowPipe } from './pipes/from-now.pipe';

@NgModule({
  declarations: [
    PageContainerComponent,
    UsernamePipe,
    AvatarComponent,
    AvatarPipe,
    SpinnerComponent,
    PageHeaderComponent,
    InlineEditInputComponent,
    FromNowPipe,
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
    FromNowPipe,
  ],
})
export class StaticModule {}
