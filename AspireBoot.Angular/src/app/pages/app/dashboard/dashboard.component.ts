import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ListUserResponse } from '../../../core/models/users/list-user-response.model';
import { ListUsersRequest } from '../../../core/models/users/list-users-request.model';
import { SessionsService } from '../../../core/services/sessions.service';
import { UsersService } from '../../../core/services/users.service';

@Component({
  selector: 'app-dashboard',
  imports: [FormsModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent {
  private sessionsService = inject(SessionsService);
  private usersService = inject(UsersService);
  private router = inject(Router);

  errorMessage = signal<string | null>(null);
  isLoading = signal(false);
  searchEmail = '';
  users = signal<ListUserResponse[]>([]);

  list(): void {
    this.errorMessage.set(null);
    this.isLoading.set(true);
    this.usersService.list({ email: this.searchEmail } as ListUsersRequest).subscribe({
      next: (res) => {
        this.users.set(res.data ?? []);
        this.isLoading.set(false);
      },
      error: (err) => {
        switch (err.status) {
          case 400: case 401: case 404:
            this.errorMessage.set(err.error?.detail ?? err.error?.errorMessage);
            break;
          default:
            this.errorMessage.set('Cannot list users now. Please try again.');
            break;
        }

        this.isLoading.set(false);
      }
    });
  }

  signOut(): void {
    this.sessionsService.delete().subscribe({
      next: () => this.handleSignOut(),
      error: () => this.handleSignOut()
    });
  }

  private handleSignOut(): void {
    this.sessionsService.clearTokens();
    void this.router.navigate(['/']);
  }

}
