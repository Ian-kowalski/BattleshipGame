@{
    ViewData["Title"] = "Battleship Game";
}

<h2>Battleship Game</h2>

<game-board></game-board>
<button id="makeMove">Make Move</button>

@section Scripts {
    <script src="~/js/signalr/dist/browser/signalr.js"></script>
    <script src="~/js/components/game-board.js"></script>
    <script>
        document.addEventListener("DOMContentLoaded", function () {
            const connection = new signalR.HubConnectionBuilder()
                .withUrl("/gameHub")
                .build();

            connection.on("ReceiveMove", function (user, x, y) {
                console.log(user + ' made a move at (' + x + ',' + y + ')');
                const board = document.querySelector('game-board');
                const isHit = Math.random() > 0.5; // Example logic
                if (isHit) {
                    board.markHit(x, y);
                } else {
                    board.markMiss(x, y);
                }
            });

            connection.on("ReceiveMessage", function (user, message) {
                console.log(user + ': ' + message);
            });

            connection.start().then(function () {
                console.log('SignalR connected');

                const board = document.querySelector('game-board');
                board.addEventListener('cellClick', function (event) {
                    const { x, y } = event.detail;
                    console.log(`Cell clicked at (${x}, ${y})`);
                    // Send move to the server
                    connection.invoke("SendMove", "Player1", x, y)
                        .catch(function (err) {
                            console.error("Error sending move:", err);
                        });
                });

                // Example of marking a ship on the own board
                board.markShip(0, 0);
                board.markShip(0, 1);
                board.markShip(0, 2);
            }).catch(function (err) {
                console.error("Error starting SignalR connection:", err);
            });
        });
    </script>
}
