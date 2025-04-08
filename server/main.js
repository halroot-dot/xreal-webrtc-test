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

// Track connected clients count
let connectedClients = 0;

// Function to verify client count
function verifyClientCount() {
  const actualClientCount = Array.from(wss.clients).length;
  if (actualClientCount !== connectedClients) {
    console.warn(`[${new Date().toISOString()}] Client count mismatch:`);
    console.warn(`- Tracked count: ${connectedClients}`);
    console.warn(`- Actual count: ${actualClientCount}`);
    connectedClients = actualClientCount; // 修正
  }
}

wss.on('connection', function (ws, req) {
  connectedClients++;
  const clientIp = req.socket.remoteAddress;
  console.log(`[${new Date().toISOString()}] New client connected`);
  console.log(`- Client IP: ${clientIp}`);
  console.log(`- Total connected clients: ${connectedClients}`);
  verifyClientCount(); // 接続時に確認

  ws.on('message', function (message, isBinary) {
    try {
      const data = JSON.parse(message);
      console.log(`[${new Date().toISOString()}] Message received:`);
      console.log(`- Type: ${data.type}`);
      console.log(`- Size: ${message.length} bytes`);
      console.log(`- Content: ${JSON.stringify(data, null, 2)}`);

      // Broadcast to all clients except sender
      let broadcastCount = 0;
      wss.clients.forEach(function (client) {
        if (client !== ws && client.readyState === WebSocket.OPEN) {
          client.send(JSON.stringify(data));
          broadcastCount++;
        }
      });
      console.log(`- Broadcasted to ${broadcastCount} clients`);
    } catch (error) {
      console.error(`[${new Date().toISOString()}] Error processing message:`);
      console.error(`- Error: ${error.message}`);
      console.error(`- Raw message: ${message}`);
    }
  });

  ws.on('error', (error) => {
    console.error(`[${new Date().toISOString()}] WebSocket error:`);
    console.error(`- Client IP: ${clientIp}`);
    console.error(`- Error: ${error.message}`);
    verifyClientCount(); // エラー時に確認
  });

  ws.on('close', () => {
    connectedClients--;
    console.log(`[${new Date().toISOString()}] Client disconnected`);
    console.log(`- Client IP: ${clientIp}`);
    console.log(`- Remaining connected clients: ${connectedClients}`);
    verifyClientCount(); // 切断時に確認
  });
});
