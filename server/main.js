const WebSocket = require('ws');
const express = require('express');
const path = require('path');
const app = express();
const port = 3001;

// Serve static files from the public directory
app.use(express.static(path.join(__dirname, 'public')));

// Create HTTP server
const server = app.listen(port, '0.0.0.0', () => {
  console.log(`Server running at http://0.0.0.0:${port}`);
});

// Create WebSocket server attached to HTTP server
const wss = new WebSocket.Server({ server });

wss.on('connection', function (ws) {
  console.log('Connected');

  ws.on('message', function (message, isBinary) {
    const data = JSON.parse(message);
    console.log('Received:', data.type);

    // Broadcast to all clients except sender
    wss.clients.forEach(function (client) {
      if (client !== ws && client.readyState === WebSocket.OPEN) {
        client.send(JSON.stringify(data));
      }
    });
  });

  ws.on('close', () => {
    console.log('Client disconnected');
  });
});
