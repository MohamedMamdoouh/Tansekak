import { Component } from '@angular/core';
import { PROFILE } from '../../profile.config';
import { SocialLinksComponent } from '../../components/social-links/social-links.component';

@Component({
  selector: 'app-designer',
  standalone: true,
  imports: [SocialLinksComponent],
  template: `
    <div class="container">
      <section class="page-section designer-page">
        <div class="designer-profile">
          <img
            class="designer-photo"
            [src]="profile.photoSrc"
            [alt]="'صورة ' + profile.name"
            width="200"
            height="200"
          />
          <div class="designer-heading">
            <h1 class="section-title">{{ profile.name }}</h1>
            <p class="designer-title">{{ profile.title }}</p>
          </div>
          <p class="designer-bio">{{ profile.bio }}</p>
          <app-social-links [links]="profile.socialLinks" />
        </div>
      </section>
    </div>
  `,
  styles: [
    `
      .designer-page {
        display: flex;
        justify-content: center;
      }

      .designer-profile {
        max-width: 520px;
        text-align: center;
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 1rem;
      }

      .designer-photo {
        width: 200px;
        height: 200px;
        border-radius: 50%;
        object-fit: cover;
        object-position: center top;
        border: 4px solid var(--color-surface-alt);
        box-shadow: 0 8px 24px rgba(30, 58, 138, 0.15);
      }

      .designer-heading {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 0.25rem;
      }

      .designer-heading .section-title {
        margin-bottom: 0;
      }

      .designer-title {
        margin: 0;
        color: var(--color-primary);
        font-weight: 600;
        font-size: 1.05rem;
      }

      .designer-bio {
        margin: 0;
        color: var(--color-text-muted);
        line-height: 1.8;
        font-size: 1.05rem;
        white-space: pre-line;
      }
    `,
  ],
})
export class DesignerComponent {
  profile = PROFILE;
}
