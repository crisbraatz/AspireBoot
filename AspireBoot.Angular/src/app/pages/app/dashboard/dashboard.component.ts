import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ListUsersRequest } from '../../../core/models/users/list-user-request.model';
import { ListUserResponse } from '../../../core/models/users/list-user-response.model';
import { AuthService } from '../../../core/services/auth.service';
import { UsersService } from '../../../core/services/users.service';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, FormsModule],
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
    this.usersService.listBy({email: this.searchEmail } as ListUsersRequest).subscribe({
      next: (res) => {
        this.users = res.data ?? [];
        this.isLoading = false;
      },
      error: (err) => {
        switch (err.status) {
          case 400: case 401: case 404:
            this.errorMessage = err.error?.errorMessage;
            break;
          default:
            this.errorMessage = 'Cannot list users now. Please try again.';
            break;
        }

        this.isLoading = false;
      }
    });
  }

  signout(): void {
    this.authService.signOut().subscribe({
      next: () => {
        this.handleSignOut();
      },
      error: () => {
        this.handleSignOut();
      }
    });
  }

  private handleSignOut(): void {
    this.authService.clearTokens();
    this.router.navigate(['/']);
  }

}
