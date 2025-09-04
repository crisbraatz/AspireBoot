export const environment = {
  production: false,
  baseUrl: 'https://localhost:5100/api',
  endpoints: {
    auth: {
      test: '/auth/test',
      refreshToken: '/auth/refresh-token',
      signIn: '/auth/signin',
      signOut: '/auth/signout',
      signUp: '/auth/signup',
    }
  }
};
