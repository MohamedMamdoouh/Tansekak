import { Component, inject } from '@angular/core';
import { ImportUploadService } from './import-upload.service';

@Component({
  selector: 'app-import-upload-overlay',
  standalone: true,
  template: `
    @if (upload.active()) {
      <div class="upload-backdrop" aria-hidden="true"></div>

      <section
        class="upload-panel"
        role="dialog"
        aria-modal="true"
        aria-labelledby="upload-title"
        aria-describedby="upload-desc"
      >
        <div class="upload-glow" aria-hidden="true"></div>

        <div class="upload-icon-wrap" aria-hidden="true">
          <div class="upload-icon-ring"></div>
          <svg class="upload-icon" viewBox="0 0 24 24" fill="none">
            <path
              d="M12 16V4m0 0L8 8m4-4 4 4M4 17v2a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-2"
              stroke="currentColor"
              stroke-width="2"
              stroke-linecap="round"
              stroke-linejoin="round"
            />
          </svg>
        </div>

        <h2 id="upload-title" class="upload-title">جاري استيراد البيانات</h2>
        <p id="upload-desc" class="upload-desc">
          {{ upload.statusLabel() }}
        </p>

        <div class="upload-progress-track" aria-hidden="true">
          <div
            class="upload-progress-fill"
            [style.width.%]="upload.progress()"
          ></div>
          <div class="upload-progress-shine"></div>
        </div>

        <div class="upload-percent">{{ upload.progress() }}%</div>

        <p class="upload-warning">
          لا تغلق هذه الصفحة ولا تحدّثها حتى ينتهي الاستيراد.
        </p>

        <button
          type="button"
          class="upload-cancel-btn"
          (click)="askToLeave()"
        >
          إلغاء الاستيراد
        </button>
      </section>
    }

    @if (upload.showLeaveDialog()) {
      <div class="leave-backdrop" (click)="stay()"></div>

      <section
        class="leave-dialog"
        role="alertdialog"
        aria-modal="true"
        aria-labelledby="leave-title"
        aria-describedby="leave-desc"
      >
        <div class="leave-icon-wrap" aria-hidden="true">⚠️</div>
        <h3 id="leave-title" class="leave-title">إيقاف الاستيراد؟</h3>
        <p id="leave-desc" class="leave-desc">
          لو غادرت الصفحة أو حدّثتها الآن، سيتوقف رفع الملف ولن تكتمل
          العملية.
        </p>

        <div class="leave-actions">
          <button type="button" class="leave-stay-btn" (click)="stay()">
            متابعة الاستيراد
          </button>
          <button type="button" class="leave-leave-btn" (click)="leave()">
            إيقاف والمغادرة
          </button>
        </div>
      </section>
    }
  `,
  styles: [
    `
      .upload-backdrop,
      .leave-backdrop {
        position: fixed;
        inset: 0;
        z-index: 2000;
        background: rgba(15, 23, 42, 0.72);
        backdrop-filter: blur(8px);
      }

      .upload-panel,
      .leave-dialog {
        position: fixed;
        left: 50%;
        top: 50%;
        transform: translate(-50%, -50%);
        z-index: 2001;
        width: min(92vw, 440px);
        padding: 2rem 1.75rem 1.5rem;
        border-radius: 1.35rem;
        background: linear-gradient(165deg, #ffffff 0%, #f8fbff 100%);
        border: 1px solid rgba(37, 99, 235, 0.14);
        box-shadow:
          0 28px 80px rgba(15, 23, 42, 0.28),
          0 0 0 1px rgba(255, 255, 255, 0.65) inset;
        text-align: center;
        animation: panel-rise 0.35s ease;
      }

      .upload-glow {
        position: absolute;
        inset: -40% auto auto 50%;
        width: 220px;
        height: 220px;
        transform: translateX(-50%);
        border-radius: 50%;
        background: radial-gradient(
          circle,
          rgba(37, 99, 235, 0.18) 0%,
          transparent 70%
        );
        pointer-events: none;
      }

      .upload-icon-wrap {
        position: relative;
        width: 84px;
        height: 84px;
        margin: 0 auto 1rem;
      }

      .upload-icon-ring {
        position: absolute;
        inset: 0;
        border-radius: 50%;
        border: 3px solid rgba(37, 99, 235, 0.12);
        border-top-color: var(--color-primary-light);
        animation: spin 1.1s linear infinite;
      }

      .upload-icon {
        position: absolute;
        inset: 0;
        margin: auto;
        width: 34px;
        height: 34px;
        color: var(--color-primary);
      }

      .upload-title {
        margin: 0 0 0.35rem;
        font-family: var(--font-display);
        font-size: 1.35rem;
        color: var(--color-text);
      }

      .upload-desc {
        margin: 0 0 1.25rem;
        color: var(--color-text-muted);
        font-size: 0.95rem;
      }

      .upload-progress-track {
        position: relative;
        height: 12px;
        border-radius: 999px;
        background: #e5edf9;
        overflow: hidden;
        margin-bottom: 0.65rem;
      }

      .upload-progress-fill {
        height: 100%;
        border-radius: inherit;
        background: linear-gradient(
          90deg,
          var(--color-primary) 0%,
          var(--color-primary-light) 55%,
          #60a5fa 100%
        );
        transition: width 0.35s ease;
        box-shadow: 0 0 18px rgba(37, 99, 235, 0.35);
      }

      .upload-progress-shine {
        position: absolute;
        inset: 0;
        background: linear-gradient(
          90deg,
          transparent 0%,
          rgba(255, 255, 255, 0.45) 50%,
          transparent 100%
        );
        animation: shine 1.8s ease-in-out infinite;
      }

      .upload-percent {
        font-family: var(--font-display);
        font-size: 2rem;
        font-weight: 800;
        line-height: 1;
        color: var(--color-primary);
        margin-bottom: 0.85rem;
      }

      .upload-warning {
        margin: 0 0 1rem;
        padding: 0.75rem 0.9rem;
        border-radius: 0.75rem;
        background: #fff7ed;
        color: #9a3412;
        border: 1px solid #fed7aa;
        font-size: 0.88rem;
        line-height: 1.5;
      }

      .upload-cancel-btn {
        border: none;
        background: transparent;
        color: #64748b;
        font: inherit;
        font-size: 0.88rem;
        font-weight: 600;
        cursor: pointer;
        text-decoration: underline;
        text-underline-offset: 3px;
      }

      .upload-cancel-btn:hover {
        color: #334155;
      }

      .leave-dialog {
        z-index: 2002;
        animation: panel-rise 0.28s ease;
      }

      .leave-icon-wrap {
        width: 64px;
        height: 64px;
        margin: 0 auto 0.85rem;
        display: grid;
        place-items: center;
        border-radius: 50%;
        background: linear-gradient(180deg, #fff7ed 0%, #ffedd5 100%);
        border: 1px solid #fdba74;
        font-size: 1.75rem;
      }

      .leave-title {
        margin: 0 0 0.5rem;
        font-family: var(--font-display);
        font-size: 1.25rem;
        color: #9a3412;
      }

      .leave-desc {
        margin: 0 0 1.25rem;
        color: #64748b;
        line-height: 1.6;
        font-size: 0.94rem;
      }

      .leave-actions {
        display: grid;
        gap: 0.65rem;
      }

      .leave-stay-btn,
      .leave-leave-btn {
        border: none;
        border-radius: 999px;
        padding: 0.78rem 1rem;
        font: inherit;
        font-weight: 700;
        cursor: pointer;
      }

      .leave-stay-btn {
        background: linear-gradient(
          135deg,
          var(--color-primary) 0%,
          var(--color-primary-light) 100%
        );
        color: #fff;
        box-shadow: 0 10px 24px rgba(37, 99, 235, 0.28);
      }

      .leave-leave-btn {
        background: #fff;
        color: #b45309;
        border: 1px solid #fdba74;
      }

      @keyframes panel-rise {
        from {
          opacity: 0;
          transform: translate(-50%, calc(-50% + 14px));
        }
        to {
          opacity: 1;
          transform: translate(-50%, -50%);
        }
      }

      @keyframes spin {
        to {
          transform: rotate(360deg);
        }
      }

      @keyframes shine {
        0% {
          transform: translateX(-120%);
        }
        100% {
          transform: translateX(120%);
        }
      }

      @media (prefers-reduced-motion: reduce) {
        .upload-panel,
        .leave-dialog,
        .upload-progress-fill,
        .upload-icon-ring,
        .upload-progress-shine {
          animation: none;
          transition: none;
        }
      }
    `,
  ],
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
