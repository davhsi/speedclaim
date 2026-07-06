const target = process.env.API_URL || 'http://localhost:5062';

module.exports = [
  {
    context: ['/api'],
    target,
    secure: false,
    changeOrigin: true,
  },
  {
    // All uploaded files (KYC docs, claim docs, avatars, survey photos) are
    // served by the backend from wwwroot/uploads. Do NOT proxy bare paths like
    // /claims or /kyc — those are Angular SPA routes and proxying them breaks
    // page refresh / deep links.
    context: ['/uploads'],
    target,
    secure: false,
    changeOrigin: true,
  },
  {
    // SignalR hub for real-time notifications (WebSocket upgrade).
    context: ['/hubs'],
    target,
    secure: false,
    changeOrigin: true,
    ws: true,
  },
];
