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
import { ScrollShadowVericalDirective } from './directives/scroll-shadow-vertical.directive';
import { ScrollShadowDirective } from './directives/scroll-shadow.directive';
import { AvatarPipe } from './pipes/avatar.pipe';
import { FromNowPipe } from './pipes/from-now.pipe';
import { PrettyDatePipe } from './pipes/pretty-date.pipe';
import { TaskStatusPipe } from './pipes/task-status.pipe';
import { UsernamePipe } from './pipes/username.pipe';
import { AutocompleteChipsComponent } from './components/autocomplete-chips/autocomplete-chips.component';
import { ImageCropperComponent } from './components/image-cropper/image-cropper.component';
import { AvatarFontSizePipe } from './pipes/avatar-font-size.pipe';
import { PxPipe } from './pipes/px.pipe';
import { EditorComponent } from './components/editor/editor.component';
import { FormInputComponent } from './components/form-input/form-input.component';
import { FormTextAreaComponent } from './components/form-textarea/form-textarea.component';
import { FormSelectComponent } from './components/form-select/form-select.component';

@NgModule({
  declarations: [
    AvatarPipe,
    AvatarFontSizePipe,
    PageContainerComponent,
    UsernamePipe,
    PxPipe,
    AvatarComponent,
    SpinnerComponent,
    PageHeaderComponent,
    InlineEditInputComponent,
    FromNowPipe,
    PrettyDatePipe,
    TaskStatusPipe,
    ScrollShadowDirective,
    ScrollShadowVericalDirective,
    InlineTextAreaComponent,
    CardListComponent,
    CardListItemComponent,
    AutocompleteChipsComponent,
    ImageCropperComponent,
    EditorComponent,
    FormInputComponent,
    FormTextAreaComponent,
    FormSelectComponent,
  ],
  imports: [CommonModule, SharedModule],
  exports: [
    PageContainerComponent,
    UsernamePipe,
    PxPipe,
    AvatarComponent,
    SpinnerComponent,
    PageHeaderComponent,
    InlineEditInputComponent,
    FromNowPipe,
    PrettyDatePipe,
    TaskStatusPipe,
    ScrollShadowDirective,
    ScrollShadowVericalDirective,
    InlineTextAreaComponent,
    CardListComponent,
    CardListItemComponent,
    AutocompleteChipsComponent,
    ImageCropperComponent,
    EditorComponent,
    FormInputComponent,
    FormTextAreaComponent,
    FormSelectComponent,
  ],
})
export class StaticModule {}
