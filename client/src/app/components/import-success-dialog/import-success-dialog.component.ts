import {
  Component,
  DestroyRef,
  EventEmitter,
  HostListener,
  Input,
  Output,
  inject,
} from '@angular/core';

const AUTO_CLOSE_MS = 5000;

@Component({
  selector: 'app-import-success-dialog',
  standalone: true,
  templateUrl: './import-success-dialog.component.html',
  styleUrl: './import-success-dialog.component.scss',
})
export class ImportSuccessDialogComponent {
  private destroyRef = inject(DestroyRef);

  @Input() heading = 'تم الاستيراد بنجاح';
  @Input() message = '';
  @Input() detailTitle = '';
  @Input() detailSubtitle = '';

  @Output() closed = new EventEmitter<void>();

  countdown = AUTO_CLOSE_MS / 1000;

  private intervalId?: ReturnType<typeof setInterval>;
  private timeoutId?: ReturnType<typeof setTimeout>;
  private dismissed = false;

  constructor() {
    this.intervalId = setInterval(() => {
      if (this.countdown > 1) {
        this.countdown -= 1;
      }
    }, 1000);

    this.timeoutId = setTimeout(() => this.dismiss(), AUTO_CLOSE_MS);

    this.destroyRef.onDestroy(() => this.clearTimers());
  }

  get countdownLabel(): string {
    if (this.countdown <= 1) {
      return 'سيتم الإغلاق خلال ثانية';
    }
    if (this.countdown === 2) {
      return 'سيتم الإغلاق خلال ثانيتين';
    }
    return `سيتم الإغلاق خلال ${this.countdown} ثواني`;
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.dismiss();
  }

  dismiss(): void {
    if (this.dismissed) return;
    this.dismissed = true;
    this.clearTimers();
    this.closed.emit();
  }

  private clearTimers(): void {
    if (this.intervalId) {
      clearInterval(this.intervalId);
      this.intervalId = undefined;
    }
    if (this.timeoutId) {
      clearTimeout(this.timeoutId);
      this.timeoutId = undefined;
    }
  }
}
