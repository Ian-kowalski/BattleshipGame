document.addEventListener("DOMContentLoaded", function () {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/gameHub")
        .build();

    connection.on("ReceiveMove", function (user, x, y) {
        // Handle receiving a move
        console.log(user + ' made a move at (' + x + ',' + y + ')');
    });

    connection.on("ReceiveMessage", function (user, message) {
        // Handle receiving a message
        console.log(user + ': ' + message);
    });

    connection.start().then(function () {
        console.log('SignalR connected');

        // Example of sending a move
        document.getElementById('makeMove').addEventListener('click', function () {
            const x = 1, y = 1; // Example coordinates
            connection.invoke("SendMove", "Player1", x, y);
        });
    }).catch(function (err) {
        return console.error(err.toString());
    });

    // Assuming gameBoardElement is your game board element
    const gameBoardElement = document.querySelector('game-board');
    gameBoardElement.initializeBoards(); // Ensure this method exists and is exposed
});
