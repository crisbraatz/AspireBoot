import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CreateUserRequest } from '../../core/models/users/create-user-request.model';
import { SessionsService } from '../../core/services/sessions.service';
import { UsersService } from '../../core/services/users.service';

@Component({
  selector: 'app-sign-up',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './sign-up.component.html',
  styleUrl: './sign-up.component.css'
})
export class SignUpComponent implements OnInit {
  private activatedRoute = inject(ActivatedRoute);
  private destroyRef = inject(DestroyRef);
  private formBuilder = inject(FormBuilder);
  private router = inject(Router);
  private sessionService = inject(SessionsService);
  private usersService = inject(UsersService);

  errorMessage = signal<string | null>(null);
  form = this.formBuilder.group({
    email: ['', [
      Validators.required,
      Validators.pattern(/^[\w!#$%&'*+/=?`{|}~^.-]+(?:\.[\w!#$%&'*+/=?`{|}~^.-]+)*@(?:[a-zA-Z0-9-]+\.)+[a-zA-Z]{2,6}$/)
    ]],
    password1: ['', [
      Validators.required,
      Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d])[a-zA-Z\d\W]{16,32}$/)
    ]],
    password2: ['', [Validators.required]]
  }, { validators: this.passwordsMatchValidator });
  isLoading = signal(false);
  returnUrl = '/app/dashboard';
  submitted = signal(false);

  get email() { return this.form.get('email'); }
  get password1() { return this.form.get('password1'); }
  get password2() { return this.form.get('password2'); }

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
    const { email, password1 } = this.form.value;
    this.usersService.create({ email, password: password1 } as CreateUserRequest).subscribe({
      next: (res) => {
        const token = res.token;
        if (token) {
          this.sessionService.storeToken(token);
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
            this.errorMessage.set('Cannot sign up now. Try again later.');
            break;
        }

        this.resetForm();
      }
    });
  }

  private passwordsMatchValidator(group: AbstractControl) {
    const password1 = group.get('password1')?.value;
    const password2 = group.get('password2')?.value;

    return password1 === password2 ? null : { passwordsMismatch: true };
  }

  private resetForm(): void {
    this.form.reset();
    this.form.markAsUntouched();
    this.isLoading.set(false);
    this.submitted.set(false);
  }

}
