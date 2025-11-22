import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { SignInRequest } from '../../core/models/users/sign-in-user-request.model';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-sign-in',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './sign-in.component.html',
  styleUrl: './sign-in.component.css'
})
export class SignInComponent implements OnInit {
  private activatedRoute = inject(ActivatedRoute);
  private authService = inject(AuthService);
  private formBuilder = inject(FormBuilder);
  private router = inject(Router);

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
  errorMessage: string | null = null;
  isLoading = false;
  returnUrl = '/app/dashboard';
  submitted = false;

  ngOnInit(): void {
    this.activatedRoute.queryParams.subscribe(params => {
      this.returnUrl = params['returnUrl'] || '/app/dashboard';
    });
  }

  submit(): void {
    this.submitted = true;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    const { email, password } = this.form.value;
    this.authService.signIn({ email, password } as SignInRequest).subscribe({
      next: (res) => {
        const token = res.data?.token;
        if (token) {
          this.authService.storeToken(token);
          this.router.navigateByUrl(this.returnUrl);
        }

        this.resetForm();
      },
      error: (err) => {
        this.errorMessage = this.getErrorMessage(err);
        this.resetForm();
      }
    });
  }

  private getErrorMessage(err: HttpErrorResponse): string {
    switch (err.status) {
      case 400: case 401: case 404: case 409:
        return err.error?.errorMessage;
      default:
        return 'Cannot sign in now. Try again later.';
    }
  }

  private resetForm(): void {
    this.form.reset();
    this.form.markAsUntouched();
    this.isLoading = false;
    this.submitted = false;
  }

  get email() { return this.form.get('email'); }
  get password() { return this.form.get('password'); }

}
