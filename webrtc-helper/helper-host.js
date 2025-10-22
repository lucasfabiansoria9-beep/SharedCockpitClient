const WebSocket = require("ws");
const SimplePeer = require("simple-peer");
const wrtc = require("wrtc");

const SIGNAL_SERVER = "ws://localhost:8765";
const signalSocket = new WebSocket(SIGNAL_SERVER);

let peer = null;
let csharpSocket = null;

// Servidor WebSocket local para recibir datos del cliente C#
const csharpServer = new WebSocket.Server({ port: 8081 });
console.log("[HOST] WebSocket escuchando en ws://localhost:8081");

csharpServer.on("connection", (socket) => {
    console.log("[HOST] Cliente C# conectado");
    csharpSocket = socket;

    socket.on("message", (msg) => {
        console.log("[HOST] Datos desde C#:", msg.toString());
        if (peer && peer.connected) {
            peer.send(msg.toString());
        }
    });

    socket.on("close", () => console.log("[HOST] Cliente C# desconectado"));
});

// Señalización con el servidor central
signalSocket.on("open", () => console.log("[SIGNAL] Conectado al servidor de señalización"));
signalSocket.on("message", (message) => {
    const msg = JSON.parse(message);
    if (msg.type === "peer-joined" && !peer) {
        console.log("[SIGNAL] Otro peer se unió, creando peer (initiator)");
        createPeer(true);
    }
    if (msg.type === "signal" && msg.data) {
        if (!peer) createPeer(false);
        peer.signal(msg.data);
    }
});

function createPeer(initiator) {
    if (peer) return;
    peer = new SimplePeer({ initiator, trickle: true, wrtc });
    peer.on("signal", (data) => signalSocket.send(JSON.stringify({ type: "signal", data })));
    peer.on("connect", () => console.log("[RTC] Conexión P2P establecida con client"));
    peer.on("data", (data) => {
        console.log("[HOST] Datos recibidos del peer:", data.toString());
        if (csharpSocket && csharpSocket.readyState === WebSocket.OPEN) {
            csharpSocket.send(data.toString());
        }
    });
}
