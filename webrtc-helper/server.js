const WebSocket = require("ws");

const wss = new WebSocket.Server({ port: 8765 });
const clients = new Set();

wss.on("connection", (ws) => {
    clients.add(ws);

    // Avisar a todos los demás que un nuevo peer se unió
    clients.forEach(c => {
        if (c !== ws && c.readyState === WebSocket.OPEN) {
            c.send(JSON.stringify({ type: "peer-joined" }));
        }
    });

    ws.on("message", (msg) => {
        // Reenviar señales a todos
        clients.forEach(c => {
            if (c !== ws && c.readyState === WebSocket.OPEN) {
                c.send(msg.toString());
            }
        });
    });

    ws.on("close", () => clients.delete(ws));
});

console.log("Signalling server corriendo en ws://localhost:8765");
