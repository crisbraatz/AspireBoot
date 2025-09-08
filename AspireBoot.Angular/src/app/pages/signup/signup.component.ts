import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth/auth.service';

@Component({
  selector: 'app-signup',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './signup.component.html',
  styleUrl: './signup.component.css'
})
export class SignUpComponent implements OnInit {
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
      password1: ['', [
        Validators.required,
        Validators.pattern('^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)[a-zA-Z\\d]{8,16}$')
      ]],
      password2: ['', [Validators.required]]
    }, { validators: this.passwordsMatchValidator });
  }

  submit(): void {
    this.authService.removeToken();
    this.submitted = true;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { email, password1 } = this.form.value;
    this.authService.signUp({ email, password: password1 }).subscribe({
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
          case 409:
            this.errorMessage = err.error.errorMessage;
            break;
          default:
            this.errorMessage = 'Can not signup now. Try again later.';
            break;
        }
      }
    });
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
