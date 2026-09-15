import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { map } from 'rxjs';

export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.ensureAdminSession().pipe(
    map((isAdmin) =>
      isAdmin ? true : router.createUrlTree(['/admin/login']),
    ),
  );
};
