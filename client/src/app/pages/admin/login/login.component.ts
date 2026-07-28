import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../auth.service';

@Component({
  selector: 'app-admin-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="container">
      <div class="card login">
        <h1>Tansekak — تسجيل الدخول</h1>
        <form [formGroup]="form" (ngSubmit)="submit()">
          <div class="form-group">
            <label>البريد الإلكتروني</label>
            <input formControlName="email" type="email" />
          </div>
          <div class="form-group">
            <label>كلمة المرور</label>
            <input formControlName="password" type="password" />
          </div>
          @if (error) { <div class="error">{{ error }}</div> }
          <button class="btn btn-primary" type="submit" [disabled]="form.invalid || loading">دخول</button>
        </form>
      </div>
    </div>
  `,
  styles: ['.login { max-width: 420px; margin: 0 auto; }']
})
export class AdminLoginComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);

  loading = false;
  error = '';
  form = this.fb.group({
    email: ['admin@tansekak.local', [Validators.required, Validators.email]],
    password: ['Admin@12345', Validators.required]
  });

  submit(): void {
    if (this.form.invalid) return;
    this.loading = true;
    this.auth.login(this.form.value.email!, this.form.value.password!).subscribe({
      next: () => this.router.navigate(['/admin']),
      error: () => {
        this.error = 'بيانات الدخول غير صحيحة.';
        this.loading = false;
      }
    });
  }
}
