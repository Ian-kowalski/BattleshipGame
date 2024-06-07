class GameBoard extends HTMLElement {
    constructor() {
        super();
        this.attachShadow({ mode: 'open' });
        
        this.render();
    }
    loadStylesheet() {
        const link = document.createElement('link');
        link.setAttribute('rel', 'stylesheet');
        link.setAttribute('href', '../css/game-board.css');
        this.shadowRoot.appendChild(link);
    }

    render() {
        this.shadowRoot.innerHTML = `
              <div class="board-container">
                <div class="board own-board" id="ownBoard">
                    ${this.createBoard()}
                </div>
                <div class="board opponent-board" id="opponentBoard">
                    ${this.createBoard()}
                </div>
            </div>
        `;

        // Add event listeners for the opponent's board cells
        const opponentBoard = this.shadowRoot.getElementById('opponentBoard');
        opponentBoard.querySelectorAll('.cell').forEach(cell => {
            cell.addEventListener('click', (event) => this.handleCellClick(event));
            cell.addEventListener('mouseover', (event) => this.handleCellMouseOver(event));
            cell.addEventListener('mouseout', (event) => this.handleCellMouseOut(event));
        });
        this.loadStylesheet(); // Call loadStylesheet method here
    }

    createBoard() {
        let cells = '';
        for (let i = 0; i < 10; i++) {
            for (let j = 0; j < 10; j++) {
                cells += `<div class="cell" data-x="${i}" data-y="${j}"></div>`;
            }
        }
        return cells;
    }

    handleCellClick(event) {
        const cell = event.target;
        const x = cell.getAttribute('data-x');
        const y = cell.getAttribute('data-y');
        this.dispatchEvent(new CustomEvent('cellClick', {
            detail: { x, y }
        }));
    }

    handleCellMouseOver(event) {
        const cell = event.target;
        cell.classList.add('highlight');
    }

    handleCellMouseOut(event) {
        const cell = event.target;
        cell.classList.remove('highlight');
    }

    markShip(x, y) {
        const cell = this.shadowRoot.querySelector(`.own-board .cell[data-x="${x}"][data-y="${y}"]`);
        cell.classList.add('ship');
    }

    markHit(x, y) {
        const cell = this.shadowRoot.querySelector(`.opponent-board .cell[data-x="${x}"][data-y="${y}"]`);
        cell.classList.add('hit');
    }

    markMiss(x, y) {
        const cell = this.shadowRoot.querySelector(`.opponent-board .cell[data-x="${x}"][data-y="${y}"]`);
        cell.classList.add('miss');
    }
}

customElements.define('game-board', GameBoard);
