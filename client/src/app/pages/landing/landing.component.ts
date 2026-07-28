import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="container">
      <section class="page-section hero-gradient">
        <h1 class="hero-title">
          قبل ما تسجل رغباتك… اعرف كليتك المتوقعة من مجموعك
        </h1>
        <p class="hero-lead">
          تنسيقك بيقارن مجموعك بحدود القبول الرسمية ويقولك ايه الكليات اللي ممكن
          تدخلها — عشان تسجل رغباتك وانت واثق.
        </p>

        <div class="step-path" aria-label="مسار التنسيق">
          <div class="step-path-item">
            <div class="step-path-icon" aria-hidden="true">
              <svg
                width="22"
                height="22"
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
            <span class="step-path-label">مجموعك</span>
          </div>
          <span class="step-path-arrow" aria-hidden="true">←</span>
          <div class="step-path-item">
            <div class="step-path-icon" aria-hidden="true">
              <svg
                width="22"
                height="22"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2"
              >
                <path d="M4 6h16M4 12h16M4 18h10" />
              </svg>
            </div>
            <span class="step-path-label">رغباتك</span>
          </div>
          <span class="step-path-arrow" aria-hidden="true">←</span>
          <div class="step-path-item">
            <div class="step-path-icon" aria-hidden="true">
              <svg
                width="22"
                height="22"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2"
              >
                <path d="M12 14l9-5-9-5-9 5 9 5z" />
                <path
                  d="M12 14l6.16-3.422a12.083 12.083 0 01.665 6.479A11.952 11.952 0 0012 20.055a11.952 11.952 0 00-6.824-2.998 12.078 12.078 0 01.665-6.479L12 14z"
                />
              </svg>
            </div>
            <span class="step-path-label">كليتك</span>
          </div>
        </div>

        <div class="hero-actions">
          <a routerLink="/predict" class="btn btn-primary btn-lg">اعرف كليتك</a>
          <a routerLink="/thanaweya-result" class="btn btn-secondary btn-lg"
            >نتيجة الثانوية</a
          >
          <a routerLink="/guide" class="btn btn-secondary btn-lg"
            >افهم التنسيق</a
          >
        </div>
      </section>

      <section class="page-section">
        <h2 class="section-title">إزاي بيشتغل؟</h2>
        <p class="section-subtitle">تلات خطوات بسيطة وانت جاهز</p>
        <div class="grid grid-3">
          <article class="card step-card">
            <span class="step-card-num">1</span>
            <h3>اكتب مجموعك وشعبتك</h3>
            <p>
              ادخل مجموعك الكلي واختار شعبتك — علمي علوم، علمي رياضة، أو أدبي.
            </p>
          </article>
          <article class="card step-card">
            <span class="step-card-num">2</span>
            <h3>نشوفلك الكليات المتاحة</h3>
            <p>
              بنقارن مجموعك بحدود القبول الرسمية ونوريلك الكليات اللي ممكن
              تدخلها.
            </p>
          </article>
          <article class="card step-card">
            <span class="step-card-num">3</span>
            <h3>روح سجل رغباتك وانت واثق</h3>
            <p>
              بعد ما تعرف اختياراتك، سجل رغباتك على موقع التنسيق الإلكتروني
              الرسمي بثقة.
            </p>
          </article>
        </div>
      </section>

      <section class="page-section">
        <h2 class="section-title">ليه تنسيقك؟</h2>
        <p class="section-subtitle">أداة مجانية بتساعدك تاخد قرار أحسن</p>
        <ul class="feature-list">
          <li>
            <span class="feature-icon" aria-hidden="true">📊</span>
            <div>
              <strong>بيانات حدود القبول محدثة</strong>
              <p class="text-muted" style="margin: 0.25rem 0 0">
                بنعتمد على حدود القبول الرسمية لآخر سنة تنسيق.
              </p>
            </div>
          </li>
          <li>
            <span class="feature-icon" aria-hidden="true">⚡</span>
            <div>
              <strong>مجاني وسريع</strong>
              <p class="text-muted" style="margin: 0.25rem 0 0">
                مش محتاج تسجل — اكتب مجموعك وشوف النتائج في ثواني.
              </p>
            </div>
          </li>
          <li>
            <span class="feature-icon" aria-hidden="true">💬</span>
            <div>
              <strong>بالعربي وبلغة تفهمها</strong>
              <p class="text-muted" style="margin: 0.25rem 0 0">
                كل حاجة بالعامية المصرية — من غير مصطلحات معقدة.
              </p>
            </div>
          </li>
        </ul>
      </section>

      <section class="page-section">
        <div class="disclaimer-box">
          <strong>تنبيه مهم:</strong> النتائج توقعية مش رسمية — القرار النهائي
          لموقع التنسيق الإلكتروني الرسمي.
        </div>
      </section>

      <section class="page-section">
        <div class="cta-banner">
          <h2>جاهز تعرف كليتك؟</h2>
          <p>اكتب مجموعك دلوقتي وشوف الكليات المتاحة ليك</p>
          <a routerLink="/predict" class="btn btn-primary btn-lg">اعرف كليتك</a>
        </div>
      </section>
    </div>
  `,
})
export class LandingComponent {}
