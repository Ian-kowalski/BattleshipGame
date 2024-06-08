class GameBoard extends HTMLElement {
    constructor() {
        super();
        this.attachShadow({ mode: 'open' });
        this.render();
        this.setupSignalR();
        this.boardsInitialized = false;
        this.playerId = '';
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
                <div id="boards">
                    <div id="yourBoard" class="board"></div>
                    <div id="trackingBoard" class="board"></div>
                </div>
            </div>
        `;

        this.initializeBoards();
        this.initializeChat();
    }

    setPlayerInfo(playerId) {
        console.log(`Setting player info: ${playerId}`);
        this.playerId = playerId;
    }

    initializeBoards() {
        if (this.boardsInitialized) return;

        console.log('Initializing boards');
        const yourBoard = this.shadowRoot.getElementById('yourBoard');
        const trackingBoard = this.shadowRoot.getElementById('trackingBoard');

        for (let i = 0; i < 100; i++) {
            const yourCell = document.createElement('div');
            yourCell.className = 'cell';
            yourBoard.appendChild(yourCell);

            const trackingCell = document.createElement('div');
            trackingCell.className = 'cell';
            trackingCell.classList.add('trackingCell');
            trackingBoard.appendChild(trackingCell);

            trackingCell.addEventListener('click', () => {
                const x = Math.floor(i / 10);
                const y = i % 10;
                this.makeMove(x, y);
            });
        }

        this.boardsInitialized = true;
    }

    initializeChat() {
        console.log('Initializing chat');
        const sendButton = this.shadowRoot.getElementById('sendButton');
        const messageInput = this.shadowRoot.getElementById('messageInput');

        sendButton.addEventListener('click', () => {
            const message = messageInput.value;
            if (message) {
                console.log(`Sending message: ${message}`);
                this.connection.invoke('SendMessage', this.playerId, message)
                    .catch(err => console.error(err));
                messageInput.value = '';
            }
        });
    }

    setBoardState(boardState) {
        console.log(`Setting board state:`, boardState);
        const yourBoard = this.shadowRoot.getElementById('yourBoard');
        const trackingBoard = this.shadowRoot.getElementById('trackingBoard');

        const { PlayerBoard, TrackingBoard, CurrentTurnPlayerId } = boardState;

        if (!PlayerBoard || !TrackingBoard) return;

        [...yourBoard.children].forEach((cell, index) => {
            const x = Math.floor(index / 10);
            const y = index % 10;
            const cellState = PlayerBoard[x][y];
            cell.className = 'cell';
            if (cellState === 1) cell.classList.add('ship');
            if (cellState === 2) cell.classList.add('hit');
            if (cellState === 3) cell.classList.add('miss');
        });

        [...trackingBoard.children].forEach((cell, index) => {
            const x = Math.floor(index / 10);
            const y = index % 10;
            const cellState = TrackingBoard[x][y];
            cell.className = 'cell trackingCell';
            if (cellState === 2) cell.classList.add('hit');
            if (cellState === 3) cell.classList.add('miss');
        });

        console.log(`Current Turn Player ID: ${CurrentTurnPlayerId}`);
    }

    setupSignalR() {
        console.log('Setting up SignalR');
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/gameHub")
            .build();

        this.connection.start()
            .then(() => console.log('Connected to SignalR'))
            .catch(err => console.error(err));

        this.connection.on('ReceiveMessage', (user, message) => {
            const messagesDiv = this.shadowRoot.getElementById('messages');
            const messageElement = document.createElement('div');
            messageElement.textContent = `${user}: ${message}`;
            messagesDiv.appendChild(messageElement);
            messagesDiv.scrollTop = messagesDiv.scrollHeight;
        });

        this.connection.on('ReceiveMove', (user, x, y) => {
            console.log(`Received move: ${user} moved to (${x}, ${y})`);
            // Handle the move, update the board
        });

        this.connection.on('UpdateCurrentTurn', (currentTurnPlayerId) => {
            console.log(`Updated current turn player ID: ${currentTurnPlayerId}`);
            // Update the UI to show whose turn it is
            const turnIndicator = this.shadowRoot.getElementById('turnIndicator');
            if (currentTurnPlayerId === this.playerId) {
                turnIndicator.textContent = "It's your turn!";
            } else {
                turnIndicator.textContent = "Waiting for opponent's move...";
            }
        });


        this.connection.on('ReceiveGameState', (gameState) => {
            console.log(`Received game state:`, gameState);
            this.setBoardState(gameState);
        });

        this.connection.on('PlayerJoined', (userId, playerCount) => {
            console.log(`Player joined: ${userId} (Total: ${playerCount})`);
            // Handle new player joining, possibly refresh the game state
        });
    }

    makeMove(x, y) {
        console.log(`Making move at (${x}, ${y})`);
        this.connection.invoke('SendMove', this.playerId, x, y)
            .catch(err => console.error(err));
    }
}

customElements.define('game-board', GameBoard);
