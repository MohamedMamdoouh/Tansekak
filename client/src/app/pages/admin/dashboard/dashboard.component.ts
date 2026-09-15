import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../../services/api.service';
import { resolveApiError } from '../../../utils/api-error.util';
import { HttpErrorResponse } from '@angular/common/http';
import { Dashboard } from '../../../models';

interface StatCard {
  key: keyof Dashboard;
  label: string;
}

interface TrackCutoffStat {
  key: 'scienceCutoffsCount' | 'mathematicsCutoffsCount' | 'literatureCutoffsCount';
  label: string;
}

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
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

  trackCutoffStats: TrackCutoffStat[] = [
    { key: 'scienceCutoffsCount', label: 'حدود علمي علوم' },
    { key: 'mathematicsCutoffsCount', label: 'حدود علمي رياضة' },
    { key: 'literatureCutoffsCount', label: 'حدود أدبي' },
  ];

  hasMissingTrackCutoffs(data: Dashboard): boolean {
    return (
      data.scienceCutoffsCount === 0 ||
      data.mathematicsCutoffsCount === 0 ||
      data.literatureCutoffsCount === 0
    );
  }

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
      error: (err: HttpErrorResponse) => {
        this.loading = false;
        this.data = null;
        this.loadError = resolveApiError(err, 'تعذر تحميل بيانات لوحة الإدارة.');
      },
    });
  }
}
