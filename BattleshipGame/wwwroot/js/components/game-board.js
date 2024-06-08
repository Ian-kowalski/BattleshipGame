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
        this.playerId = playerId;
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
        const sendButton = this.shadowRoot.getElementById('sendButton');
        const messageInput = this.shadowRoot.getElementById('messageInput');

        sendButton.addEventListener('click', () => {
            const message = messageInput.value;
            if (message) {
                this.connection.invoke("SendMessage", this.playerId, message);
                messageInput.value = '';
            }
        });
    }

    setupSignalR() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/gameHub")
            .build();

        this.connection.on("ReceiveMove", (user, x, y, hit) => {
            this.displayMessage(user, `made a move at (${x}, ${y}) - Hit: ${hit}`);
            this.updateBoard('trackingBoard', x, y, hit);
        });

        this.connection.on("ReceiveMessage", (user, message) => {
            this.displayMessage(user, message);
        });

        this.connection.start().then(() => {
            console.log('SignalR connected');
        }).catch(err => {
            console.error(err.toString());
        });
    }

    displayMessage(user, message) {
        const messagesDiv = this.shadowRoot.getElementById('messages');
        const messageElement = document.createElement('div');
        messageElement.textContent = `${user}: ${message}`;
        messagesDiv.appendChild(messageElement);
    }

    updateBoard(boardId, x, y, hit) {
        const board = this.shadowRoot.getElementById(boardId);
        const cellIndex = x * 10 + y;
        const cell = board.querySelectorAll('.cell')[cellIndex];

        if (hit) {
            cell.classList.add('hit');
        } else {
            cell.classList.add('miss');
        }
    }

    makeMove(x, y) {
        fetch('/Game/MakeMove', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify({ x, y })
        })
            .then(response => response.json())
            .then(data => {
                if (data.hit) {
                    console.log(`Hit at (${x}, ${y})`);
                } else {
                    console.log(`Miss at (${x}, ${y})`);
                }
            })
            .catch(error => {
                console.error('Error making move:', error);
            });
    }

    setBoardState(boardState) {
        const { playerBoard, trackingBoard } = boardState;

        this.updateBoardState('yourBoard', playerBoard);
        this.updateBoardState('trackingBoard', trackingBoard);
    }

    updateBoardState(boardId, boardState) {
        const boardElement = this.shadowRoot.getElementById(boardId);
        const cells = boardElement.querySelectorAll('.cell');

        for (let i = 0; i < cells.length; i++) {
            const x = Math.floor(i / 10);
            const y = i % 10;

            if (boardState && boardState.length > x && boardState[x].length > y) {
                cells[i].className = 'cell'; // Reset the cell classes

                switch (boardState[x][y]) {
                    case 1: // Ship
                        if (boardId === 'yourBoard') {
                            cells[i].classList.add('ship');
                        }
                        break;
                    case 2: // Hit
                        cells[i].classList.add('hit');
                        break;
                    case 3: // Miss
                        cells[i].classList.add('miss');
                        break;
                }
            }
        }
    }
}

customElements.define('game-board', GameBoard);
