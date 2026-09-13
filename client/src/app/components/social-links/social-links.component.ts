import { Component, input } from '@angular/core';
import { SocialLink } from '../../profile.config';

@Component({
  selector: 'app-social-links',
  standalone: true,
  templateUrl: './social-links.component.html',
  styleUrl: './social-links.component.scss',
})
export class SocialLinksComponent {
  links = input.required<SocialLink[]>();
  size = input<'sm' | 'md'>('md');
}
