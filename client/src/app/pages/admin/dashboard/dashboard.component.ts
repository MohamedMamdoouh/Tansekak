import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../../api.service';
import { Dashboard } from '../../../models';

interface StatCard {
  key: keyof Dashboard;
  label: string;
}

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="container dashboard">
      @if (loading) {
        <div class="loading-grid" aria-busy="true" aria-label="جاري التحميل">
          @for (i of [1, 2, 3, 4]; track i) {
            <div class="skeleton-card"></div>
          }
        </div>
      } @else if (loadError) {
        <div class="error-panel card">
          <h1>تعذر تحميل النظرة العامة</h1>
          <p>{{ loadError }}</p>
          <button type="button" class="btn btn-primary" (click)="load()">
            إعادة المحاولة
          </button>
        </div>
      } @else if (data) {
        <section class="hero-band" aria-labelledby="dashboard-title">
          <div class="hero-copy">
            <h1 id="dashboard-title">نظرة عامة على بيانات القبول</h1>
          </div>

          @if (data.currentYear) {
            <div class="year-anchor" aria-label="سنة القبول الحالية">
              <span class="year-anchor-label">سنة القبول النشطة</span>
              <span class="year-anchor-value">{{ data.currentYear }}</span>
            </div>
          } @else {
            <div class="year-anchor year-anchor--empty">
              <span class="year-anchor-label">لا توجد سنة قبول نشطة</span>
              <span class="year-anchor-meta"
                >راجع بيانات السنوات قبل الاستيراد</span
              >
            </div>
          }
        </section>

        <section class="stats-grid" aria-label="إحصائيات النظام">
          @for (stat of statCards; track stat.key) {
            <article class="stat-card card">
              <span class="stat-value">{{ data[stat.key] | number }}</span>
              <h3 class="stat-label">{{ stat.label }}</h3>
            </article>
          }
        </section>

        <section class="actions-section" aria-label="إجراءات سريعة">
          <h2 class="actions-title">ماذا تريد أن تفعل؟</h2>
          <div class="actions-grid">
            <a routerLink="/admin/cutoffs" class="action-card card">
              <div class="action-icon" aria-hidden="true">
                <svg
                  width="24"
                  height="24"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="2"
                >
                  <path
                    d="M9 7h6M9 12h6M9 17h4M5 5h14a2 2 0 012 2v10a2 2 0 01-2 2H5a2 2 0 01-2-2V7a2 2 0 012-2z"
                  />
                </svg>
              </div>
              <div>
                <h3>إدارة حدود القبول</h3>
                <p>إضافة، تعديل، أو حذف سجل حد أدنى لكلية وشعبة.</p>
              </div>
              <span class="action-arrow" aria-hidden="true">←</span>
            </a>

            <a routerLink="/admin/import" class="action-card card">
              <div class="action-icon action-icon--accent" aria-hidden="true">
                <svg
                  width="24"
                  height="24"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="2"
                >
                  <path d="M12 3v12M7 8l5-5 5 5M5 21h14" />
                </svg>
              </div>
              <div>
                <h3>استيراد حدود القبول</h3>
                <p>رفع ملف Markdown لشعبة واحدة واستبدال حدود السنة الحالية.</p>
              </div>
              <span class="action-arrow" aria-hidden="true">←</span>
            </a>

            <a routerLink="/admin/import-results" class="action-card card action-card--unavailable">
              <div class="action-icon action-icon--accent" aria-hidden="true">
                <svg
                  width="24"
                  height="24"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="2"
                >
                  <path d="M12 3v12M7 8l5-5 5 5M5 21h14" />
                </svg>
              </div>
              <div>
                <h3>استيراد نتائج الثانوية</h3>
                <p>رفع ملف Excel بنتائج الطلاب واستبدال بيانات السنة المختارة.</p>
                <p class="action-unavailable">خدمة الاستيراد غير متاحة حالياً. حاول مرة أخرى لاحقاً.</p>
              </div>
              <span class="action-arrow" aria-hidden="true">←</span>
            </a>
          </div>
        </section>
      }
    </div>
  `,
  styles: [
    `
      .dashboard {
        display: grid;
        gap: 1.5rem;
      }

      .hero-band {
        display: grid;
        gap: 1.5rem;
        padding: 2rem;
        border-radius: 20px;
        background: linear-gradient(
          135deg,
          #0f172a 0%,
          #1e3a8a 58%,
          #1e40af 100%
        );
        color: #fff;
        box-shadow: 0 16px 40px rgba(15, 23, 42, 0.22);
      }

      @media (min-width: 768px) {
        .hero-band {
          grid-template-columns: 1fr auto;
          align-items: center;
        }
      }

      .hero-band h1 {
        font-family: var(--font-display);
        margin: 0;
        font-size: clamp(1.5rem, 3vw, 2rem);
        line-height: 1.35;
      }

      .year-anchor {
        display: grid;
        gap: 0.25rem;
        justify-items: center;
        text-align: center;
        min-width: 148px;
        padding: 1.25rem 1.5rem;
        border-radius: 18px;
        background: rgba(255, 255, 255, 0.1);
        border: 1px solid rgba(255, 255, 255, 0.16);
      }

      .year-anchor--empty {
        justify-items: stretch;
        text-align: right;
      }

      .year-anchor-label {
        font-size: 0.82rem;
        opacity: 0.82;
      }

      .year-anchor-value {
        font-family: var(--font-display);
        font-size: clamp(2.5rem, 5vw, 3.25rem);
        font-weight: 800;
        line-height: 1;
        color: var(--color-accent-light);
      }

      .year-anchor-meta {
        font-size: 0.85rem;
        opacity: 0.78;
      }

      .stats-grid {
        display: grid;
        gap: 1rem;
        grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
      }

      .stat-card {
        padding: 1.25rem;
        border: 1px solid rgba(30, 58, 138, 0.06);
        transition:
          transform 0.2s ease,
          box-shadow 0.2s ease;
      }

      .stat-card:hover {
        transform: translateY(-2px);
        box-shadow: 0 12px 28px rgba(15, 23, 42, 0.1);
      }

      .stat-value {
        display: block;
        font-family: var(--font-display);
        font-size: clamp(1.75rem, 3vw, 2.1rem);
        font-weight: 800;
        color: var(--color-primary);
        line-height: 1;
        margin-bottom: 0.5rem;
      }

      .stat-label {
        margin: 0;
        font-size: 0.98rem;
        color: var(--color-text);
      }

      .actions-title {
        font-family: var(--font-display);
        margin: 0 0 1rem;
        font-size: 1.15rem;
        color: var(--color-primary);
      }

      .actions-grid {
        display: grid;
        gap: 1rem;
      }

      @media (min-width: 768px) {
        .actions-grid {
          grid-template-columns: repeat(2, 1fr);
        }
      }

      .action-card {
        display: grid;
        grid-template-columns: auto 1fr auto;
        gap: 1rem;
        align-items: center;
        padding: 1.25rem 1.35rem;
        border: 1px solid rgba(30, 58, 138, 0.08);
        transition:
          transform 0.2s ease,
          box-shadow 0.2s ease,
          border-color 0.2s ease;
      }

      .action-card:hover {
        transform: translateY(-2px);
        box-shadow: 0 12px 28px rgba(15, 23, 42, 0.1);
        border-color: rgba(37, 99, 235, 0.22);
      }

      .action-icon {
        width: 48px;
        height: 48px;
        border-radius: 14px;
        display: grid;
        place-items: center;
        background: var(--color-surface-alt);
        color: var(--color-primary);
      }

      .action-icon--accent {
        background: #fff7ed;
        color: var(--color-accent);
      }

      .action-card h3 {
        margin: 0 0 0.35rem;
        font-family: var(--font-display);
        font-size: 1.02rem;
        color: var(--color-primary);
      }

      .action-card p {
        margin: 0;
        color: var(--color-text-muted);
        font-size: 0.9rem;
        line-height: 1.6;
      }

      .action-unavailable {
        margin-top: 0.5rem !important;
        color: #92400e !important;
        font-weight: 600;
      }

      .action-card--unavailable {
        opacity: 0.85;
      }

      .action-arrow {
        color: var(--color-accent);
        font-size: 1.25rem;
        font-weight: 700;
      }

      .loading-grid {
        display: grid;
        gap: 1rem;
        grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
      }

      .skeleton-card {
        height: 120px;
        border-radius: 16px;
        background: linear-gradient(
          90deg,
          #eef2f7 25%,
          #f8fafc 50%,
          #eef2f7 75%
        );
        background-size: 200% 100%;
        animation: shimmer 1.4s ease-in-out infinite;
      }

      .error-panel {
        text-align: center;
        padding: 2.5rem 1.5rem;
      }

      .error-panel h1 {
        font-family: var(--font-display);
        margin: 0 0 0.75rem;
        color: var(--color-primary);
        font-size: 1.35rem;
      }

      .error-panel p {
        margin: 0 0 1.25rem;
        color: var(--color-text-muted);
      }

      @media (prefers-reduced-motion: reduce) {
        .stat-card:hover,
        .action-card:hover {
          transform: none;
        }

        .skeleton-card {
          animation: none;
          background: #eef2f7;
        }
      }
    `,
  ],
})
export class AdminDashboardComponent implements OnInit {
  private api = inject(ApiService);

  data: Dashboard | null = null;
  loading = true;
  loadError = '';

  statCards: StatCard[] = [
    { key: 'governoratesCount', label: 'المحافظات' },
    { key: 'universitiesCount', label: 'الجامعات والمعاهد' },
    { key: 'facultiesCount', label: 'الكليات' },
    { key: 'universityFacultiesCount', label: 'كليات بكل جامعة ومعهد' },
    { key: 'studentResultsCount', label: 'نتائج الثانوية' },
  ];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.loadError = '';
    this.api.getDashboard().subscribe({
      next: (data) => {
        this.data = data;
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.data = null;
        this.loadError =
          err.status === 401
            ? 'انتهت الجلسة. سجل الدخول مرة أخرى.'
            : (err.error?.message ?? 'تعذر تحميل بيانات لوحة الإدارة.');
      },
    });
  }
}
