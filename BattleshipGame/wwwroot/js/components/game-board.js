class GameBoard extends HTMLElement {
    constructor() {
        super();
        this.attachShadow({ mode: 'open' });
        this.render();
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

        console.log('Game board initialized.');

        //this.initializeBoards();
        this.initializeChat();
    }

    initializeBoards() {
        const yourBoard = this.shadowRoot.getElementById('yourBoard');
        const trackingBoard = this.shadowRoot.getElementById('trackingBoard');

        console.log('Initializing game boards...');

        for (let i = 0; i < 100; i++) {
            const yourCell = document.createElement('div');
            yourCell.className = 'cell';
            yourBoard.appendChild(yourCell);

            const trackingCell = document.createElement('div');
            trackingCell.className = 'cell';
            trackingBoard.appendChild(trackingCell);

            // Add hover effect only for tracking board cells
            trackingCell.addEventListener('mouseover', () => {
                trackingCell.classList.add('hover');
            });

            trackingCell.addEventListener('mouseout', () => {
                trackingCell.classList.remove('hover');
            });
        }

        console.log('Game boards initialized.');
    }

    initializeChat() {
        const sendButton = this.shadowRoot.getElementById('sendButton');
        const messageInput = this.shadowRoot.getElementById('messageInput');
        const messages = this.shadowRoot.getElementById('messages');

        console.log('Initializing chat...');

        sendButton.addEventListener('click', () => {
            const message = messageInput.value;
            if (message) {
                const messageElement = document.createElement('div');
                messageElement.textContent = message;
                messages.appendChild(messageElement);
                messageInput.value = '';
                messages.scrollTop = messages.scrollHeight; // Auto-scroll to the bottom
            }
        });

        messageInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                sendButton.click();
            }
        });

        console.log('Chat initialized.');
    }

    setBoardState(boardState) {
        console.log('Received board state:', boardState);

        // Parse boardState if it's a JSON string
        if (typeof boardState === 'string') {
            try {
                boardState = JSON.parse(boardState);
            } catch (e) {
                console.error('Error parsing board state JSON:', e);
                return;
            }
        }

        // Ensure boardState is a 2D array
        if (!Array.isArray(boardState) || !Array.isArray(boardState[0])) {
            console.error('Invalid board state format.');
            return;
        }

        const yourBoard = this.shadowRoot.getElementById('yourBoard');
        const cells = yourBoard.querySelectorAll('.cell');

        for (let i = 0; i < cells.length; i++) {
            const x = Math.floor(i / 10);
            const y = i % 10;

            // Ensure x and y are within bounds of boardState
            if (x < boardState.length && y < boardState[x].length) {
                if (boardState[x][y] === 1) { // Assuming 1 represents 'Ship' in the enum
                    cells[i].classList.add('ship');
                }
            } else {
                console.error('Coordinates out of bounds:', x, y);
            }
        }
    }
}

customElements.define('game-board', GameBoard);
