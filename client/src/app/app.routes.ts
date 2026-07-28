import { Routes } from '@angular/router';
import { LandingComponent } from './pages/landing/landing.component';
import { PredictComponent } from './pages/predict/predict.component';
import { GuideComponent } from './pages/guide/guide.component';
import { ResultsComponent } from './pages/results/results.component';
import { ThanaweyaResultComponent } from './pages/thanaweya-result/thanaweya-result.component';
import { AdminLoginComponent } from './pages/admin/login/login.component';
import { AdminLayoutComponent } from './pages/admin/admin-layout.component';
import { AdminDashboardComponent } from './pages/admin/dashboard/dashboard.component';
import { AdminCutoffsComponent } from './pages/admin/cutoffs/cutoffs.component';
import { AdminImportComponent } from './pages/admin/import/import.component';
import { AdminImportResultsComponent } from './pages/admin/import-results/import-results.component';
import { adminGuard } from './admin.guard';

export const routes: Routes = [
  { path: '', component: LandingComponent },
  { path: 'predict', component: PredictComponent },
  { path: 'thanaweya-result', component: ThanaweyaResultComponent },
  { path: 'guide', component: GuideComponent },
  { path: 'results', component: ResultsComponent },
  { path: 'admin/login', component: AdminLoginComponent },
  {
    path: 'admin',
    canActivate: [adminGuard],
    component: AdminLayoutComponent,
    children: [
      { path: '', component: AdminDashboardComponent },
      { path: 'cutoffs', component: AdminCutoffsComponent },
      { path: 'import', component: AdminImportComponent },
      { path: 'import-results', component: AdminImportResultsComponent },
    ],
  },
  { path: '**', redirectTo: '' }
];
