import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../services/auth/auth.service';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent {
  errorMessage: string | null = null;
  pingMessage: string | null = null;

  constructor(private authService: AuthService, private router: Router) {

  }

  test(): void {
    this.authService.test().subscribe({
      next: (res) => {
        this.pingMessage = res.data!;
      },
      error: (err) => {
        switch (err.status) {
          default:
            this.errorMessage = 'Can not ping now. Try again later.';
            break;
        }
      }
    });
  }

  signout(): void {
    this.authService.signOut().subscribe({
      next: () => {
        this.authService.removeToken();
        this.router.navigate(['/']);
      },
      error: () => {
        this.authService.removeToken();
        this.router.navigate(['/']);
      }
    });
  }

}
