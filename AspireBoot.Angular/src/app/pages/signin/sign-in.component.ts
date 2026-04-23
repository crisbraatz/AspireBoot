import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CreateSessionRequest } from '../../core/models/sessions/create-session-request.model';
import { SessionsService } from '../../core/services/sessions.service';

@Component({
  selector: 'app-sign-in',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './sign-in.component.html',
  styleUrl: './sign-in.component.css'
})
export class SignInComponent implements OnInit {
  private activatedRoute = inject(ActivatedRoute);
  private destroyRef = inject(DestroyRef);
  private formBuilder = inject(FormBuilder);
  private router = inject(Router);
  private sessionsService = inject(SessionsService);

  errorMessage = signal<string | null>(null);
  form = this.formBuilder.group({
    email: ['', [
      Validators.required,
      Validators.pattern(/^[\w!#$%&'*+/=?`{|}~^.-]+(?:\.[\w!#$%&'*+/=?`{|}~^.-]+)*@(?:[a-zA-Z0-9-]+\.)+[a-zA-Z]{2,6}$/)
    ]],
    password: ['', [
      Validators.required,
      Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d])[a-zA-Z\d\W]{16,32}$/)
    ]]
  });
  isLoading = signal(false);
  returnUrl = '/app/dashboard';
  submitted = signal(false);

  get email() { return this.form.get('email'); }
  get password() { return this.form.get('password'); }

  ngOnInit(): void {
    this.activatedRoute.queryParams
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(params => { this.returnUrl = params['returnUrl'] || '/app/dashboard'; });
  }

  submit(): void {
    this.submitted.set(true);
    if (this.form.invalid) {
      this.form.markAllAsTouched();

      return;
    }

    this.errorMessage.set(null);
    this.isLoading.set(true);
    const { email, password } = this.form.value;
    this.sessionsService.create({ email, password } as CreateSessionRequest).subscribe({
      next: (res) => {
        const token = res.token;
        if (token) {
          this.sessionsService.storeToken(token);
          void this.router.navigateByUrl(this.returnUrl);
        }

        this.resetForm();
      },
      error: (err) => {
        switch (err.status) {
          case 400: case 401: case 404: case 409:
            this.errorMessage.set(err.error?.detail ?? err.error?.errorMessage);
            break;
          default:
            this.errorMessage.set('Cannot sign in now. Try again later.');
            break;
        }

        this.resetForm();
      }
    });
  }

  private resetForm(): void {
    this.form.reset();
    this.form.markAsUntouched();
    this.isLoading.set(false);
    this.submitted.set(false);
  }

}
