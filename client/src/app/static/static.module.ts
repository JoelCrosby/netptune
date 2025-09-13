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
import { EntityTypePipe } from './pipes/entity-type.pipe';
import { ActivityTypePipe } from './pipes/activity-type.pipe';
import { AutocompleteChipsComponent } from './components/autocomplete-chips/autocomplete-chips.component';
import { AvatarFontSizePipe } from './pipes/avatar-font-size.pipe';
import { PxPipe } from './pipes/px.pipe';
import { EditorComponent } from './components/editor/editor.component';
import { FormInputComponent } from './components/form-input/form-input.component';
import { FormTextAreaComponent } from './components/form-textarea/form-textarea.component';
import { FormSelectComponent } from './components/form-select/form-select.component';
import { FormSelectOptionComponent } from './components/form-select/form-select-option.component';
import { FormSelectDropdownComponent } from './components/form-select/form-select-dropdown.component';
import { CommentsListComponent } from './components/comments-list/comments-list.component';
import { ColorSelectComponent } from './components/color-select/color-select.component';
import { CardComponent } from './components/card/card.component';
import { CardTitleComponent } from './components/card/card-title.component';
import { CardSubtitleComponent } from './components/card/card-subtitle.component';
import { CardHeaderComponent } from './components/card/card-header.component';
import { CardHeaderImageComponent } from './components/card/card-header-image.component';
import { CardGroupComponent } from './components/card/card-group.component';
import { AutofocusDirective } from './directives/autofocus.directive';
import { ActivityPipe } from './pipes/activity.pipe';
import { UserSelectComponent } from './components/user-select/user-select.component';
import { FormErrorComponent } from './components/form-error/form-error.component';
import { CardContentComponent } from './components/card/card-content.component';
import { CardActionsComponent } from './components/card/card-actions.component';
import { DialogContentComponent } from './components/dialog-content/dialog-content.component';
import { DialogContainerComponent } from './components/dialog/dialog-container.component';
import { DialogCloseDirective } from './directives/dialog-close.directive';
import { DialogActionsDirective } from './directives/dialog-actions.directive';

@NgModule({
  imports: [
    CommonModule,
    SharedModule,
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
    EntityTypePipe,
    ActivityTypePipe,
    ActivityPipe,
    ScrollShadowDirective,
    ScrollShadowVericalDirective,
    AutofocusDirective,
    DialogActionsDirective,
    InlineTextAreaComponent,
    CardListComponent,
    CardListItemComponent,
    AutocompleteChipsComponent,
    EditorComponent,
    FormInputComponent,
    FormTextAreaComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    FormSelectDropdownComponent,
    CommentsListComponent,
    ColorSelectComponent,
    CardComponent,
    CardTitleComponent,
    CardSubtitleComponent,
    CardHeaderComponent,
    CardHeaderImageComponent,
    CardGroupComponent,
    UserSelectComponent,
    FormErrorComponent,
    CardContentComponent,
    CardActionsComponent,
    DialogContentComponent,
    DialogContainerComponent,
    DialogCloseDirective,
  ],
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
    EntityTypePipe,
    ActivityTypePipe,
    ActivityPipe,
    ScrollShadowDirective,
    ScrollShadowVericalDirective,
    AutofocusDirective,
    DialogActionsDirective,
    InlineTextAreaComponent,
    CardListComponent,
    CardListItemComponent,
    AutocompleteChipsComponent,
    EditorComponent,
    FormInputComponent,
    FormTextAreaComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    FormSelectDropdownComponent,
    CommentsListComponent,
    ColorSelectComponent,
    CardComponent,
    CardTitleComponent,
    CardSubtitleComponent,
    CardHeaderComponent,
    CardHeaderImageComponent,
    CardContentComponent,
    CardGroupComponent,
    UserSelectComponent,
    FormErrorComponent,
    CardActionsComponent,
    DialogContentComponent,
    DialogContainerComponent,
    DialogCloseDirective,
  ],
  providers: [FromNowPipe],
})
export class StaticModule {}
