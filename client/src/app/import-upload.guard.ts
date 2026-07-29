import { CanDeactivateFn } from '@angular/router';
import { inject } from '@angular/core';
import { ImportUploadService } from './import-upload.service';

export const importUploadGuard: CanDeactivateFn<unknown> = () => {
  const upload = inject(ImportUploadService);
  if (!upload.active()) return true;
  return upload.promptLeave();
};
