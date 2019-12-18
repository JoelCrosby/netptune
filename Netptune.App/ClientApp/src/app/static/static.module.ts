import { SharedModule } from '@app/shared/shared.module';
// Angular modules
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PageContainerComponent } from './components/page-container/page-container.component';

@NgModule({
  declarations: [PageContainerComponent],
  imports: [CommonModule, SharedModule],
  exports: [PageContainerComponent],
})
export class StaticModule {}
