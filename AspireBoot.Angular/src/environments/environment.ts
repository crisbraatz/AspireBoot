export const environment = {
  baseUrl: 'https://api.localhost:1443/api',
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
