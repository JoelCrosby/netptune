import { SharedModule } from '@app/shared/shared.module';
// Angular modules
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PageContainerComponent } from './components/page-container/page-container.component';
import { UsernamePipe } from './pipes/username.pipe';

@NgModule({
  declarations: [PageContainerComponent, UsernamePipe],
  imports: [CommonModule, SharedModule],
  exports: [PageContainerComponent, UsernamePipe],
})
export class StaticModule {}
