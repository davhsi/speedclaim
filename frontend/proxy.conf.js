const target = process.env.API_URL || 'http://localhost:5062';

module.exports = [
  {
    context: ['/api'],
    target,
    secure: false,
    changeOrigin: true,
  },
];
