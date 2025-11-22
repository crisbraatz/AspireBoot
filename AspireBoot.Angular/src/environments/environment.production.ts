export const environment = {
  baseUrl: 'https://localhost:5100/api',
  endpoints: {
    auth: {
      refreshToken: '/auth/refresh-token',
      signIn: '/auth/sign-in',
      signOut: '/auth/sign-out',
      signUp: '/auth/sign-up'
    },
    users: {
        listBy: '/users/list-by'
    }
  }
};
