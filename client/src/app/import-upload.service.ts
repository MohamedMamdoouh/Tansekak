import { Injectable, signal } from '@angular/core';
import { ImportUploadPhase } from './import-file-upload';

@Injectable({ providedIn: 'root' })
export class ImportUploadService {
  readonly active = signal(false);
  readonly progress = signal(0);
  readonly phase = signal<ImportUploadPhase>('uploading');
  readonly showLeaveDialog = signal(false);

  private abortController: AbortController | null = null;
  private leaveResolver: ((confirmed: boolean) => void) | null = null;
  private readonly beforeUnloadHandler = (event: BeforeUnloadEvent) => {
    event.preventDefault();
    event.returnValue = '';
  };

  begin(): AbortSignal {
    this.abortController?.abort();
    this.abortController = new AbortController();
    this.active.set(true);
    this.progress.set(0);
    this.phase.set('uploading');
    this.showLeaveDialog.set(false);
    window.addEventListener('beforeunload', this.beforeUnloadHandler);
    return this.abortController.signal;
  }

  updateProgress(percent: number, phase: ImportUploadPhase): void {
    this.progress.set(percent);
    this.phase.set(phase);
  }

  finish(): void {
    this.active.set(false);
    this.showLeaveDialog.set(false);
    this.abortController = null;
    this.leaveResolver = null;
    window.removeEventListener('beforeunload', this.beforeUnloadHandler);
  }

  cancel(): void {
    this.abortController?.abort();
    this.finish();
  }

  promptLeave(): Promise<boolean> {
    if (!this.active()) return Promise.resolve(true);

    this.showLeaveDialog.set(true);
    return new Promise((resolve) => {
      this.leaveResolver = resolve;
    });
  }

  confirmLeave(): void {
    this.showLeaveDialog.set(false);
    this.cancel();
    this.leaveResolver?.(true);
    this.leaveResolver = null;
  }

  stayOnPage(): void {
    this.showLeaveDialog.set(false);
    this.leaveResolver?.(false);
    this.leaveResolver = null;
  }

  statusLabel(): string {
    if (this.phase() === 'processing') {
      return 'جاري المعالجة على الخادم...';
    }
    return '';
  }
}
