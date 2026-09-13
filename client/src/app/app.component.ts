import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from './services/auth.service';
import { PROFILE } from './profile.config';
import { SocialLinksComponent } from './components/social-links/social-links.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, SocialLinksComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
  
})
export class AppComponent implements OnInit {
  auth = inject(AuthService);
  profile = PROFILE;

  ngOnInit(): void {
    this.auth.loadSession().subscribe();
  }
}
