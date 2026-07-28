import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from './auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
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
          <a routerLink="/guide" routerLinkActive="active">افهم التنسيق</a>
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
        <p class="footer-credit">تم تصميم الموقع بواسطة محمد ممدوح</p>
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
    `,
  ],
})
export class AppComponent implements OnInit {
  auth = inject(AuthService);

  ngOnInit(): void {
    this.auth.loadSession().subscribe();
  }
}
