import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { ImportUploadOverlayComponent } from '../../components/import-upload-overlay/import-upload-overlay.component';
import { ImportUploadService } from '../../services/import-upload.service';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    ImportUploadOverlayComponent,
  ],
  templateUrl: './admin-layout.component.html',
  styleUrl: './admin-layout.component.scss',
  
})
export class AdminLayoutComponent {
  private auth = inject(AuthService);
  private upload = inject(ImportUploadService);

  logout(): void {
    if (this.upload.active()) {
      void this.upload.promptLeave().then((confirmed) => {
        if (confirmed) this.auth.logoutAndRedirect();
      });
      return;
    }

    this.auth.logoutAndRedirect();
  }
}
