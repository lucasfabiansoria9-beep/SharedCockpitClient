const ws = new WebSocket("ws://localhost:8081");
const statusEl = document.getElementById("status");

ws.onopen = () => {
    statusEl.textContent = "✅ Conectado al servidor local";
    statusEl.style.color = "#00ffb3";
};

ws.onclose = () => {
    statusEl.textContent = "❌ Desconectado";
    statusEl.style.color = "#ff4444";
};

ws.onmessage = (msg) => {
    try {
        const data = JSON.parse(msg.data);
        if (!data) return;

        const a = data.attitude || {};
        const p = data.position || {};
        const c = data.controls || {};
        const cab = data.cabin || {};
        const env = data.environment || {};

        document.getElementById("pitch").textContent = a.pitch?.toFixed(1) ?? "--";
        document.getElementById("bank").textContent = a.bank?.toFixed(1) ?? "--";
        document.getElementById("heading").textContent = a.heading?.toFixed(0) ?? "--";

        document.getElementById("lat").textContent = p.latitude?.toFixed(4) ?? "--";
        document.getElementById("lon").textContent = p.longitude?.toFixed(4) ?? "--";
        document.getElementById("alt").textContent = p.altitude?.toFixed(0) ?? "--";

        document.getElementById("throttle").textContent = c.throttleLever?.toFixed(0) ?? "--";
        document.getElementById("flaps").textContent = c.flapsHandlePercent?.toFixed(0) ?? "--";
        document.getElementById("rudder").textContent = c.rudder?.toFixed(2) ?? "--";

        document.getElementById("autopilot").textContent = cab.autopilotMaster ? "ON" : "OFF";
        document.getElementById("gear").textContent = cab.landingGearDown ? "DOWN" : "UP";
        document.getElementById("spoilers").textContent = cab.spoilersHandle?.toFixed(1) ?? "--";

        document.getElementById("wind").textContent = env.windSpeedKnots?.toFixed(1) ?? "--";
        document.getElementById("pressure").textContent = env.barometricPressureInHg?.toFixed(2) ?? "--";
        document.getElementById("temp").textContent = env.ambientTemperatureC?.toFixed(1) ?? "--";
    } catch (err) {
        console.error("Error procesando datos:", err);
    }
};
