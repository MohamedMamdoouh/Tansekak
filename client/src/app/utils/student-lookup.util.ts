import { HttpErrorResponse } from '@angular/common/http';
import { FormGroup } from '@angular/forms';
import { Observable } from 'rxjs';
import { StudentResult } from '../models';
import { resolveApiError } from './api-error.util';

export interface StudentLookupState {
  loading: boolean;
  error: string;
  result: StudentResult | null;
}

export function submitStudentLookup(
  form: FormGroup,
  lookup: (seatingNo: string) => Observable<StudentResult>,
  setState: (state: Partial<StudentLookupState>) => void,
): void {
  form.markAllAsTouched();
  if (form.invalid) return;

  const seatingNo = String(form.value.seatingNo ?? '').trim();
  setState({ loading: true, error: '', result: null });

  lookup(seatingNo).subscribe({
    next: (data) => setState({ loading: false, result: data }),
    error: (err: HttpErrorResponse) =>
      setState({
        loading: false,
        error: resolveApiError(err, 'حدث خطأ أثناء البحث. حاول مرة أخرى لاحقاً.'),
      }),
  });
}
