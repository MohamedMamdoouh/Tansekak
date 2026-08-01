import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from './auth.service';
import { PROFILE } from './profile.config';
import { SocialLinksComponent } from './components/social-links/social-links.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, SocialLinksComponent],
  template: `
    <header class="header">
      <div class="container header-inner">
        <a routerLink="/" class="brand">تنسيقك</a>
        <nav>
          <a
            routerLink="/"
            routerLinkActive="active"
            [routerLinkActiveOptions]="{ exact: true }"
            >الرئيسية</a
          >
          <a routerLink="/predict" routerLinkActive="active">اعرف كليتك</a>
          <a routerLink="/thanaweya-result" routerLinkActive="active"
            >نتيجة الثانوية</a
          >
          <a routerLink="/track-rank" routerLinkActive="active">ترتيب الشعبة</a>
          <a routerLink="/guide" routerLinkActive="active">افهم التنسيق</a>
          <a routerLink="/designer" routerLinkActive="active">المطور</a>
          @if (auth.isAdmin()) {
            <a routerLink="/admin" routerLinkActive="active">لوحة التحكم</a>
            <button type="button" class="nav-btn" (click)="auth.logoutAndRedirect(true)">
              خروج
            </button>
          }
        </nav>
      </div>
    </header>
    <main>
      <router-outlet />
    </main>
    <footer class="footer">
      <div class="container footer-inner">
        <p class="footer-tagline">تنسيقك — بوابتك لتوقع كليتك المثالية</p>
        <p class="footer-credit">
          تم تصميم الموقع بواسطة {{ profile.name }}
        </p>
        <app-social-links [links]="profile.socialLinks" size="sm" />
      </div>
    </footer>
  `,
  styles: [
    `
      .header {
        background: #1e3a8a;
        color: #fff;
        padding: 1rem 0;
      }
      .header-inner {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 1rem;
        flex-wrap: wrap;
      }
      .brand {
        font-family: 'Noto Kufi Arabic', 'Segoe UI', sans-serif;
        font-size: 1.5rem;
        font-weight: 700;
      }
      nav {
        display: flex;
        gap: 0.25rem;
        flex-wrap: wrap;
        align-items: center;
      }
      nav a,
      .nav-btn {
        opacity: 0.85;
        padding: 0.4rem 0.75rem;
        border-radius: 8px;
        transition:
          background 0.15s ease,
          opacity 0.15s ease;
      }
      nav a:hover,
      .nav-btn:hover {
        opacity: 1;
        background: rgba(255, 255, 255, 0.1);
      }
      nav a.active {
        opacity: 1;
        background: rgba(255, 255, 255, 0.15);
        font-weight: 600;
      }
      .nav-btn {
        background: none;
        border: none;
        color: inherit;
        cursor: pointer;
        font: inherit;
      }
      main {
        padding: 2rem 0 3rem;
        min-height: calc(100vh - 180px);
      }
      .footer {
        background: #111827;
        color: #d1d5db;
        padding: 1.5rem 0;
        text-align: center;
      }
      .footer-inner {
        display: flex;
        flex-direction: column;
        gap: 0.75rem;
        align-items: center;
      }
      .footer-tagline {
        margin: 0;
        font-size: 0.95rem;
      }
      .footer-credit {
        margin: 0;
        font-size: 0.82rem;
        opacity: 0.65;
      }

      @media (max-width: 640px) {
        .header {
          padding: 0.75rem 0;
        }
        .header-inner {
          flex-direction: column;
          align-items: stretch;
          gap: 0.65rem;
        }
        .brand {
          font-size: 1.25rem;
          text-align: center;
        }
        nav {
          display: flex;
          gap: 0.2rem;
          flex-wrap: nowrap;
          overflow-x: auto;
          -webkit-overflow-scrolling: touch;
          scrollbar-width: none;
          padding-bottom: 0.15rem;
        }
        nav::-webkit-scrollbar {
          display: none;
        }
        nav a,
        .nav-btn {
          flex-shrink: 0;
          font-size: 0.85rem;
          padding: 0.45rem 0.65rem;
          white-space: nowrap;
        }
        main {
          padding: 1.25rem 0 2rem;
          min-height: calc(100vh - 160px);
        }
        .footer {
          padding: 1.15rem 0;
        }
        .footer-tagline {
          font-size: 0.88rem;
          padding: 0 0.5rem;
        }
      }
    `,
  ],
})
export class AppComponent implements OnInit {
  auth = inject(AuthService);
  profile = PROFILE;

  ngOnInit(): void {
    this.auth.loadSession().subscribe();
  }
}
