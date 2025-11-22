import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ListUserResponse } from '../../../core/models/users/list-user-response.model';
import { AuthService } from '../../../core/services/auth.service';
import { UsersService } from '../../../core/services/users.service';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent {
  private authService = inject(AuthService);
  private usersService = inject(UsersService);
  private router = inject(Router);

  errorMessage: string | null = null;
  isLoading = false;

  searchEmail = '';
  users: ListUserResponse[] = [];

  listBy(): void {
    this.isLoading = true;
    this.usersService.listBy(this.searchEmail).subscribe({
      next: (res) => {
        this.users = res.data ?? [];
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Cannot list users now. Please try again.';
        this.isLoading = false;
      }
    });
  }

  signOut(): void {
    this.isLoading = true;
    this.authService.signOut().subscribe({
      next: () => this.handleSignOut(),
      error: () => {
        this.errorMessage = 'Cannot sign out now. Please try again.';
        this.handleSignOut();
      },
      complete: () => this.isLoading = false
    });
  }

  private handleSignOut(): void {
    this.authService.clearTokens();
    this.router.navigate(['/']);
  }

}
