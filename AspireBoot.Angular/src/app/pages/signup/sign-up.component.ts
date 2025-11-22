import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { SignUpRequest } from '../../core/models/users/sign-up-user-request.model';

@Component({
  selector: 'app-sign-up',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './sign-up.component.html',
  styleUrl: './sign-up.component.css'
})
export class SignUpComponent implements OnInit {
  private activatedRoute = inject(ActivatedRoute);
  private authService = inject(AuthService);
  private formBuilder = inject(FormBuilder);
  private router = inject(Router);

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
    const { email, password1 } = this.form.value;
    this.authService.signUp({ email, password: password1 } as SignUpRequest).subscribe({
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
      case 400: case 401: case 409:
        return err.error?.errorMessage;
      default:
        return 'Cannot sign up now. Try again later.';
    }
  }

  private resetForm(): void {
    this.form.reset();
    this.form.markAsUntouched();
    this.isLoading = false;
    this.submitted = false;
  }

  private passwordsMatchValidator(group: AbstractControl) {
    const password1 = group.get('password1')?.value;
    const password2 = group.get('password2')?.value;
    return password1 === password2 ? null : { passwordsMismatch: true };
  }

  get email() { return this.form.get('email'); }
  get password1() { return this.form.get('password1'); }
  get password2() { return this.form.get('password2'); }

}
