// Angular modules
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PageContainerComponent } from './components/page-container/page-container.component';

@NgModule({
  declarations: [PageContainerComponent],
  imports: [CommonModule],
  exports: [PageContainerComponent],
})
export class StaticModule {}
