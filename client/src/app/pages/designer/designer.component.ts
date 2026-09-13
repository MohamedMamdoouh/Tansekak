import { Component } from '@angular/core';
import { PROFILE } from '../../profile.config';
import { SocialLinksComponent } from '../../components/social-links/social-links.component';

@Component({
  selector: 'app-designer',
  standalone: true,
  imports: [SocialLinksComponent],
  templateUrl: './designer.component.html',
  styleUrl: './designer.component.scss',
  
})
export class DesignerComponent {
  profile = PROFILE;
}
