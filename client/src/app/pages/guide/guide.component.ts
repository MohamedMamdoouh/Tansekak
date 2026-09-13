import { Component, ElementRef, ViewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { GUIDE_FAQ_PAGE_SIZE } from '../../constants/pagination.constants';
import {
  ENROLLMENT_STEPS,
  GUIDE_FAQ_ITEMS,
  GuideItem,
  PREPARATION_STEPS,
} from './guide.content';

@Component({
  selector: 'app-guide',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './guide.component.html',
  styleUrl: './guide.component.scss',
})
export class GuideComponent {
  @ViewChild('accordionTop') accordionTop?: ElementRef<HTMLElement>;

  page = 1;
  readonly pageSize = GUIDE_FAQ_PAGE_SIZE;

  readonly enrollmentSteps = ENROLLMENT_STEPS;
  readonly preparationSteps = PREPARATION_STEPS;
  readonly items = GUIDE_FAQ_ITEMS;

  get totalPages(): number {
    return Math.ceil(this.items.length / this.pageSize);
  }

  get paginatedItems(): GuideItem[] {
    const start = (this.page - 1) * this.pageSize;
    return this.items.slice(start, start + this.pageSize);
  }

  goToPage(nextPage: number): void {
    if (nextPage < 1 || nextPage > this.totalPages) {
      return;
    }

    this.page = nextPage;
    this.accordionTop?.nativeElement.scrollIntoView({
      behavior: 'smooth',
      block: 'start',
    });
  }
}
