import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { SharedModule } from '@shared/shared.module';
import { AvatarComponent } from './components/avatar/avatar.component';
import { CardListItemComponent } from './components/card-list-item/card-list-item.component';
import { CardListComponent } from './components/card-list/card-list.component';
import { InlineEditInputComponent } from './components/inline-edit-input/inline-edit-input.component';
import { InlineTextAreaComponent } from './components/inline-text-area/inline-text-area.component';
import { PageContainerComponent } from './components/page-container/page-container.component';
import { PageHeaderComponent } from './components/page-header/page-header.component';
import { SpinnerComponent } from './components/spinner/spinner.component';
import { ScrollShadowDirective } from './directives/scroll-shadow.directive';
import { AvatarPipe } from './pipes/avatar.pipe';
import { FromNowPipe } from './pipes/from-now.pipe';
import { TaskStatusPipe } from './pipes/task-status.pipe';
import { UsernamePipe } from './pipes/username.pipe';

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
    TaskStatusPipe,
    ScrollShadowDirective,
    InlineTextAreaComponent,
    CardListComponent,
    CardListItemComponent,
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
    TaskStatusPipe,
    ScrollShadowDirective,
    InlineTextAreaComponent,
    CardListComponent,
    CardListItemComponent,
  ],
})
export class StaticModule {}
