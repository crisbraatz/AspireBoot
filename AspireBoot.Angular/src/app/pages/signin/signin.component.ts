import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth/auth.service';

@Component({
  selector: 'app-signin',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './signin.component.html',
  styleUrl: './signin.component.css'
})
export class SignInComponent implements OnInit {
  errorMessage: string | null = null;
  form!: FormGroup;
  returnUrl: string = '/app/dashboard';
  submitted = false;

  constructor(
    private activatedRoute: ActivatedRoute,
    private authService: AuthService,
    private formBuilder: FormBuilder,
    private router: Router) {

  }

  ngOnInit(): void {
    this.returnUrl = this.activatedRoute.snapshot.queryParamMap.get('returnUrl') || '/app/dashboard';
    this.form = this.formBuilder.group({
      email: ['', [
        Validators.required,
        Validators.pattern(/^[\w!#$%&'*+/=?`{|}~^.-]+(?:\.[\w!#$%&'*+/=?`{|}~^.-]+)*@(?:[a-zA-Z0-9-]+\.)+[a-zA-Z]{2,6}$/)
      ]],
      password: ['', [
        Validators.required,
        Validators.pattern('^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)[a-zA-Z\\d]{8,16}$')
      ]]
    });
  }

  submit(): void {
    this.authService.removeToken();
    this.submitted = true;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { email, password } = this.form.value;
    this.authService.signIn({ email, password: password }).subscribe({
      next: (res) => {
        const token = res.data?.authToken;
        if (token) {
          this.authService.setToken(token);
          this.router.navigateByUrl(this.returnUrl);
        }

        this.form.reset();
        this.form.markAsUntouched();
        this.submitted = false;
      },
      error: (err) => {
        this.form.markAsUntouched();
        this.form.updateValueAndValidity();
        this.submitted = false;
        switch (err.status) {
          case 400:
          case 401:
          case 404:
          case 409:
            this.errorMessage = err.error.errorMessage;
            break;
          default:
            this.errorMessage = 'Can not signin now. Try again later.';
            break;
        }
      }
    });
  }

  get email() { return this.form.get('email'); }
  get password() { return this.form.get('password'); }

}
