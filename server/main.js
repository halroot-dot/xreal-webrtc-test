const WebSocket = require('ws');
const wss = new WebSocket.Server({ port: 3001 });

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
