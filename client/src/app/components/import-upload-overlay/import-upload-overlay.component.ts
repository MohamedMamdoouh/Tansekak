import { Component, inject } from '@angular/core';
import { ImportUploadService } from '../../services/import-upload.service';

@Component({
  selector: 'app-import-upload-overlay',
  standalone: true,
  templateUrl: './import-upload-overlay.component.html',
  styleUrl: './import-upload-overlay.component.scss',
})
export class ImportUploadOverlayComponent {
  readonly upload = inject(ImportUploadService);

  askToLeave(): void {
    this.upload.promptLeave();
  }

  stay(): void {
    this.upload.stayOnPage();
  }

  leave(): void {
    this.upload.confirmLeave();
  }
}
