import { lyl, StyleRenderer, ThemeVariables } from '@alyle/ui';
import {
  ImgCropperConfig,
  ImgCropperEvent,
  LyImageCropper,
} from '@alyle/ui/image-cropper';
import { Platform } from '@angular/cdk/platform';
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  Output,
  ViewChild,
} from '@angular/core';
import { dataURItoBlob } from '@core/util/blob';

const STYLES = (_: ThemeVariables) => {
  return {
    cropperUpload: lyl`{
      display: flex
      align-items: center
      justify-content: space-around
    }`,
    cropperToolBar: lyl`{
      display: flex
      align-items: center
      padding-top: .2rem
    }`,
    cropper: lyl`{
      max-width: 260px
      height: 260px
      border-radius: 4px
      margin: auto
    }`,
    cropResult: lyl`{
      border-radius: 50%
    }`,
  };
};

@Component({
  selector: 'app-image-cropper',
  templateUrl: './image-cropper.component.html',
  styleUrls: ['./image-cropper.component.scss'],
  providers: [StyleRenderer],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ImageCropperComponent implements AfterViewInit {
  @Input() src: string;
  @Output() cropped = new EventEmitter<{ blob: Blob; src: string }>();
  @Output() canceled = new EventEmitter();
  @Output() cleared = new EventEmitter();

  @ViewChild(LyImageCropper, { static: true }) readonly cropper: LyImageCropper;

  classes = this.sRenderer.renderSheet(STYLES);
  croppedImage?: string;
  scale: number;
  ready: boolean;
  minScale: number;

  myConfig: ImgCropperConfig = {
    width: 250,
    height: 250,
    type: 'image/png',
    round: true,
  };

  constructor(readonly sRenderer: StyleRenderer, private platform: Platform) {}

  ngAfterViewInit() {
    if (!this.platform.isBrowser) {
      return;
    }

    if (!this.src) {
      this.ready = true;
      return;
    }

    this.cropper.setImageUrl(this.src);

    this.ready = true;
  }

  onCropped(e: ImgCropperEvent) {
    this.croppedImage = e.dataURL;
  }

  onCropClicked() {
    this.cropper.crop();

    const src = this.croppedImage;
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
