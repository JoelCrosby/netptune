import { StyleRenderer } from '@alyle/ui';
import {
  ImgCropperConfig,
  ImgCropperEvent,
  LyImageCropper,
} from '@alyle/ui/image-cropper';
import { Platform } from '@angular/cdk/platform';
import { AfterViewInit, ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject, input, output, viewChild } from '@angular/core';
import { dataURItoBlob } from '@core/util/blob';

import { MatButton } from '@angular/material/button';
import { MatTooltip } from '@angular/material/tooltip';
import { MatIcon } from '@angular/material/icon';

@Component({
    selector: 'app-image-cropper',
    templateUrl: './image-cropper.component.html',
    styleUrls: ['./image-cropper.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [LyImageCropper, MatButton, MatTooltip, MatIcon]
})
export class ImageCropperComponent implements OnInit, AfterViewInit {
  readonly sRenderer = inject(StyleRenderer);
  private platform = inject(Platform);
  private cd = inject(ChangeDetectorRef);

  readonly src = input.required<string>();
  readonly size = input.required<number>();

  readonly cropped = output<{
    blob: Blob;
    src: string;
}>();
  readonly canceled = output();
  readonly cleared = output();

  readonly cropper = viewChild.required(LyImageCropper);

  croppedImage?: string;
  scale!: number;
  ready!: boolean;
  minScale!: number;

  myConfig!: ImgCropperConfig;

  ngOnInit() {
    this.myConfig = {
      width: this.size(),
      height: this.size(),
      type: 'image/png',
      round: true,
    };
  }

  ngAfterViewInit() {
    if (!this.platform.isBrowser) {
      return;
    }

    const src = this.src();
    if (!src) {
      this.ready = true;
      this.cd.detectChanges();
      return;
    }

    this.cropper().loadImage(src);

    this.ready = true;
  }

  onCropped(e: ImgCropperEvent) {
    this.croppedImage = e.dataURL;
  }

  onCropClicked() {
    this.cropper().crop();

    const src = this.croppedImage;

    if (!src) return;

    const blob = dataURItoBlob(src);

    this.cropped.emit({ blob, src });
  }

  onCancelClicked() {
    this.canceled.emit();
  }

  onClearClicked() {
    this.cleared.emit();
  }
}
