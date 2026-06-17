class GameBoard extends HTMLElement {
    constructor() {
        super();
        this.attachShadow({ mode: 'open' });
        this.render();
        this.boardsInitialized = false;
        this.userName = '';
        this.currentTurnPlayerId = null;
        this.gameOver = false;
        this.setupSignalR();
    }

    render() {
        this.shadowRoot.innerHTML = `
            <link rel="stylesheet" href="/css/game-board.css">
            <div id="container">
                <div id="chat">
                    <div id="messages"></div>
                    <div id="chatInput">
                        <input type="text" id="messageInput" placeholder="Type a message...">
                        <button id="sendButton">Send</button>
                    </div>
                </div>
                <div id="boardLabels">
                    <span>Your Board</span>
                    <span>Tracking Board</span>
                </div>
                <div id="boards">
                    <div id="yourBoard" class="board"></div>
                    <div id="trackingBoard" class="board"></div>
                </div>
            </div>
        `;

        this.initializeBoards();
        this.initializeChat();
    }

    setPlayerInfo(userName) {
        this.userName = userName;
    }

    initializeBoards() {
        if (this.boardsInitialized) return;

        const yourBoard = this.shadowRoot.getElementById('yourBoard');
        const trackingBoard = this.shadowRoot.getElementById('trackingBoard');

        for (let i = 0; i < 100; i++) {
            const yourCell = document.createElement('div');
            yourCell.className = 'cell';
            yourBoard.appendChild(yourCell);

            const trackingCell = document.createElement('div');
            trackingCell.className = 'cell trackingCell';
            trackingBoard.appendChild(trackingCell);

            trackingCell.addEventListener('click', () => {
                if (this.gameOver) return;
                if (this.currentTurnPlayerId !== this.userName) {
                    this.addMessage('System', "It's not your turn.");
                    return;
                }
                const x = Math.floor(i / 10);
                const y = i % 10;
                this.makeMove(x, y);
            });
        }

        this.boardsInitialized = true;
    }

    initializeChat() {
        const sendButton = this.shadowRoot.getElementById('sendButton');
        const messageInput = this.shadowRoot.getElementById('messageInput');

        sendButton.addEventListener('click', () => {
            const message = messageInput.value.trim();
            if (message) {
                this.connection.invoke('SendMessage', this.userName, message)
                    .catch(err => console.error(err));
                messageInput.value = '';
            }
        });

        messageInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') sendButton.click();
        });
    }

    addMessage(user, message) {
        const messagesDiv = this.shadowRoot.getElementById('messages');
        const el = document.createElement('div');
        el.textContent = `${user}: ${message}`;
        messagesDiv.appendChild(el);
        messagesDiv.scrollTop = messagesDiv.scrollHeight;
    }

    setBoardState(boardState) {
        const yourBoard = this.shadowRoot.getElementById('yourBoard');
        const trackingBoard = this.shadowRoot.getElementById('trackingBoard');
        const { PlayerBoard, TrackingBoard, CurrentTurnPlayerId } = boardState;

        if (!PlayerBoard || !TrackingBoard) return;

        [...yourBoard.children].forEach((cell, index) => {
            const x = Math.floor(index / 10);
            const y = index % 10;
            const state = PlayerBoard[x][y];
            cell.className = 'cell';
            if (state === 1) cell.classList.add('ship');
            if (state === 2) cell.classList.add('hit');
            if (state === 3) cell.classList.add('miss');
        });

        [...trackingBoard.children].forEach((cell, index) => {
            const x = Math.floor(index / 10);
            const y = index % 10;
            const state = TrackingBoard[x][y];
            cell.className = 'cell trackingCell';
            if (state === 2) cell.classList.add('hit');
            if (state === 3) cell.classList.add('miss');
        });

        if (CurrentTurnPlayerId) {
            this.currentTurnPlayerId = CurrentTurnPlayerId;
        }
    }

    updateTrackingCell(x, y, hit) {
        const trackingBoard = this.shadowRoot.getElementById('trackingBoard');
        const index = x * 10 + y;
        const cell = trackingBoard.children[index];
        if (!cell) return;
        cell.className = 'cell trackingCell';
        cell.classList.add(hit ? 'hit' : 'miss');
    }

    updatePlayerCell(x, y, hit) {
        const yourBoard = this.shadowRoot.getElementById('yourBoard');
        const index = x * 10 + y;
        const cell = yourBoard.children[index];
        if (!cell) return;
        cell.className = 'cell';
        cell.classList.add(hit ? 'hit' : 'miss');
    }

    setupSignalR() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/gameHub")
            .build();

        this.connection.start()
            .then(() => {
                this.connection.invoke('RegisterPlayer', this.userName)
                    .catch(err => console.error(err));
            })
            .catch(err => console.error(err));

        this.connection.on('ReceiveMessage', (user, message) => {
            this.addMessage(user, message);
        });

        this.connection.on('PlayerJoined', (userId, playerCount) => {
            this.dispatchEvent(new CustomEvent('playerjoined', { detail: playerCount, bubbles: true }));
        });

        this.connection.on('UpdateCurrentTurn', (playerId) => {
            this.currentTurnPlayerId = playerId;
            this.dispatchEvent(new CustomEvent('turnchanged', { detail: playerId, bubbles: true }));
        });

        // ReceiveMove fires for all clients — check if it's our move or opponent's
        this.connection.on('ReceiveMove', (user, x, y, hit) => {
            if (user === this.userName) {
                this.updateTrackingCell(x, y, hit);
            } else {
                this.updatePlayerCell(x, y, hit);
            }
        });

        this.connection.on('GameOver', (winner) => {
            this.gameOver = true;
            this.addMessage('System', winner === this.userName ? '🎉 You won!' : `${winner} won!`);
            this.dispatchEvent(new CustomEvent('gameover', { detail: winner, bubbles: true }));
        });

        this.connection.on('GameReset', () => {
            this.dispatchEvent(new CustomEvent('gamereset', { bubbles: true }));
        });

        this.connection.on('ReceiveGameState', (gameState) => {
            this.setBoardState(gameState);
        });
    }

    makeMove(x, y) {
        this.connection.invoke('SendMove', this.userName, x, y)
            .catch(err => console.error(err));
    }
}

customElements.define('game-board', GameBoard);
