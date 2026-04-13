export const environment = {
  baseUrl: 'https://api.localhost:1443/api',
  endpoints: {
    sessions: {
      create: '/sessions',
      delete: '/sessions',
      refresh: '/sessions/refresh',
    },
    users: {
      create: '/users',
      list: '/users'
    }
  }
};
