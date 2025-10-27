(() => {
    const statusEl = document.getElementById('connection-status');
    const stateOutput = document.getElementById('state-output');
    const logList = document.getElementById('log-list');
    const poseInputs = {
        lat: document.getElementById('pose-lat'),
        lon: document.getElementById('pose-lon'),
        alt: document.getElementById('pose-alt'),
        hdg: document.getElementById('pose-hdg')
    };
    const walkaroundToggle = document.getElementById('walkaround-toggle');
    const sendPoseBtn = document.getElementById('send-pose');

    let socket;
    let sequence = 0;
    const originId = crypto.randomUUID();

    function log(message) {
        const li = document.createElement('li');
        li.textContent = `[${new Date().toLocaleTimeString()}] ${message}`;
        logList.prepend(li);
    }

    function setStatus(connected) {
        statusEl.textContent = connected ? 'Conectado' : 'Desconectado';
        statusEl.className = connected ? 'status status-connected' : 'status status-disconnected';
    }

    function connect() {
        const host = location.hostname || '127.0.0.1';
        const port = location.port || '8081';
        const url = `ws://${host}:${port}/panel`;
        log(`Conectando a ${url}`);
        socket = new WebSocket(url);

        socket.onopen = () => {
            setStatus(true);
            log('WS abierto');
        };

        socket.onclose = () => {
            setStatus(false);
            log('WS cerrado');
            setTimeout(connect, 2000);
        };

        socket.onerror = (evt) => {
            log(`Error WS: ${evt.message ?? 'desconocido'}`);
        };

        socket.onmessage = (evt) => {
            log(`RX ${evt.data}`);
            try {
                const payload = JSON.parse(evt.data);
                if (payload.type === 'snapshot') {
                    stateOutput.textContent = JSON.stringify(payload.state, null, 2);
                } else if (payload.type === 'stateChange') {
                    stateOutput.textContent = `Último cambio: ${payload.prop}=${payload.value}`;
                }
            } catch (err) {
                console.error(err);
            }
        };
    }

    function sendStateChange(prop, value) {
        if (!socket || socket.readyState !== WebSocket.OPEN) {
            log('No hay conexión activa');
            return;
        }

        const payload = {
            type: 'stateChange',
            prop,
            value,
            originId,
            sequence: ++sequence,
            serverTime: Date.now()
        };
        socket.send(JSON.stringify(payload));
        log(`TX stateChange ${prop}=${value}`);
    }

    function sendPose() {
        if (!socket || socket.readyState !== WebSocket.OPEN) {
            log('No hay conexión activa');
            return;
        }

        const pose = {
            lat: parseFloat(poseInputs.lat.value) || 0,
            lon: parseFloat(poseInputs.lon.value) || 0,
            alt: parseFloat(poseInputs.alt.value) || 0,
            hdg: parseFloat(poseInputs.hdg.value) || 0,
            pitch: 0,
            bank: 0,
            state: walkaroundToggle.checked ? 'walk' : 'idle'
        };

        const payload = {
            type: 'avatarPose',
            originId,
            sequence: ++sequence,
            pose
        };
        socket.send(JSON.stringify(payload));
        log(`TX avatarPose lat=${pose.lat} lon=${pose.lon}`);
    }

    document.querySelectorAll('nav button').forEach(btn => {
        btn.addEventListener('click', () => {
            const tab = btn.dataset.tab;
            document.querySelectorAll('nav button').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            document.querySelectorAll('.tab').forEach(section => {
                section.classList.toggle('active', section.id === `tab-${tab}`);
            });
        });
    });

    document.querySelectorAll('.action').forEach(btn => {
        btn.addEventListener('click', () => {
            const prop = btn.dataset.prop;
            let value = btn.dataset.value;
            if (value === 'toggle') {
                value = { op: 'toggle' };
            } else if (!isNaN(Number(value))) {
                value = Number(value);
            }
            sendStateChange(prop, value);
        });
    });

    sendPoseBtn.addEventListener('click', sendPose);

    connect();
})();
