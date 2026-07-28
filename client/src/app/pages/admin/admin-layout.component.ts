import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../auth.service';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <header class="admin-header">
      <div class="container header-inner">
        <a routerLink="/admin" class="brand">
          <span class="brand-mark" aria-hidden="true"></span>
          <span class="brand-text">
            <span class="brand-title">لوحة الإدارة</span>
          </span>
        </a>

        <nav class="admin-nav" aria-label="قائمة الإدارة">
          <a
            routerLink="/admin"
            routerLinkActive="active"
            [routerLinkActiveOptions]="{ exact: true }"
            >نظرة عامة</a
          >
          <a routerLink="/admin/cutoffs" routerLinkActive="active"
            >حدود القبول</a
          >
          <a routerLink="/admin/import" routerLinkActive="active">استيراد</a>
          <a routerLink="/admin/import-results" routerLinkActive="active"
            >نتائج الثانوية</a
          >
        </nav>

        <button type="button" class="logout-btn" (click)="logout()">
          تسجيل الخروج
        </button>
      </div>
    </header>
    <main class="admin-main">
      <router-outlet />
    </main>
  `,
  styles: [
    `
      .admin-header {
        position: sticky;
        top: 0;
        z-index: 50;
        background: linear-gradient(180deg, #0f172a 0%, #111827 100%);
        color: #fff;
        border-bottom: 1px solid rgba(255, 255, 255, 0.08);
        box-shadow: 0 8px 24px rgba(15, 23, 42, 0.18);
      }

      .header-inner {
        display: flex;
        align-items: center;
        gap: 1rem;
        padding: 0.85rem 0;
        flex-wrap: wrap;
      }

      .brand {
        display: inline-flex;
        align-items: center;
        gap: 0.75rem;
        color: #fff;
        min-width: 0;
      }

      .brand-mark {
        width: 10px;
        height: 36px;
        border-radius: 999px;
        background: linear-gradient(
          180deg,
          var(--color-accent-light) 0%,
          var(--color-accent) 100%
        );
        flex-shrink: 0;
      }

      .brand-text {
        display: grid;
        gap: 0.1rem;
        min-width: 0;
      }

      .brand-title {
        font-family: var(--font-display);
        font-size: 1.15rem;
        font-weight: 700;
        line-height: 1.2;
      }

      .admin-nav {
        display: flex;
        align-items: center;
        gap: 0.35rem;
        padding: 0.25rem;
        border-radius: 999px;
        background: rgba(255, 255, 255, 0.06);
        border: 1px solid rgba(255, 255, 255, 0.08);
        flex: 1;
        justify-content: center;
        min-width: 0;
      }

      .admin-nav a {
        color: rgba(255, 255, 255, 0.72);
        padding: 0.5rem 0.95rem;
        border-radius: 999px;
        font-size: 0.92rem;
        font-weight: 500;
        white-space: nowrap;
        transition:
          background 0.15s ease,
          color 0.15s ease;
      }

      .admin-nav a:hover {
        color: #fff;
        background: rgba(255, 255, 255, 0.08);
      }

      .admin-nav a.active {
        color: #fff;
        font-weight: 700;
        background: rgba(37, 99, 235, 0.35);
        box-shadow: inset 0 0 0 1px rgba(147, 197, 253, 0.25);
      }

      .logout-btn {
        margin-right: auto;
        border: 1px solid rgba(255, 255, 255, 0.16);
        background: rgba(255, 255, 255, 0.04);
        color: rgba(255, 255, 255, 0.82);
        border-radius: 999px;
        padding: 0.5rem 1rem;
        font: inherit;
        font-size: 0.88rem;
        font-weight: 600;
        cursor: pointer;
        white-space: nowrap;
        transition:
          background 0.15s ease,
          border-color 0.15s ease,
          color 0.15s ease;
      }

      .logout-btn:hover {
        background: rgba(255, 255, 255, 0.1);
        border-color: rgba(255, 255, 255, 0.24);
        color: #fff;
      }

      .logout-btn:focus-visible {
        outline: 2px solid var(--color-accent-light);
        outline-offset: 2px;
      }

      .admin-main {
        padding: 2rem 0 3rem;
        min-height: calc(100vh - 72px);
      }

      @media (max-width: 720px) {
        .header-inner {
          flex-direction: column;
          align-items: stretch;
        }

        .brand {
          justify-content: center;
        }

        .admin-nav {
          justify-content: stretch;
          overflow-x: auto;
          scrollbar-width: none;
        }

        .admin-nav::-webkit-scrollbar {
          display: none;
        }

        .admin-nav a {
          flex: 1;
          text-align: center;
        }

        .logout-btn {
          margin-right: 0;
          width: 100%;
        }
      }

      @media (prefers-reduced-motion: reduce) {
        .admin-nav a,
        .logout-btn {
          transition: none;
        }
      }
    `,
  ],
})
export class AdminLayoutComponent {
  private auth = inject(AuthService);

  logout(): void {
    this.auth.logoutAndRedirect();
  }
}
