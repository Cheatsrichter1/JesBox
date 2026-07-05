// pm2 process definition. On the server: `cd server && pm2 start ecosystem.config.js`
module.exports = {
  apps: [
    {
      name: 'jesbox',
      script: 'index.js',
      cwd: __dirname,
      env: {
        PORT: 8080,
      },
    },
  ],
};
