// chat.js

class ChatComponent extends HTMLElement {
    constructor() {
        super();

        // Create a shadow root
        this.shadow = this.attachShadow({ mode: 'open' });

        // Create main container
        this.container = document.createElement('div');
        this.container.classList.add('container');

        // Add HTML content
        this.container.innerHTML = `
            <div class="row p-1">
                <div class="col-1">User</div>
                <div class="col-5"><input type="text" id="userInput" /></div>
            </div>
            <div class="row p-1">
                <div class="col-1">Message</div>
                <div class="col-5"><input type="text" class="w-100" id="messageInput" /></div>
            </div>
            <div class="row p-1">
                <div class="col-6 text-end">
                    <input type="button" id="sendButton" value="Send Message" />
                </div>
            </div>
            <div class="row p-1">
                <div class="col-6">
                    <hr />
                </div>
            </div>
            <div class="row p-1">
                <div class="col-6">
                    <ul id="messagesList"></ul>
                </div>
            </div>
        `;

        // Append container to shadow DOM
        this.shadow.appendChild(this.container);

        // Get input elements
        this.userInput = this.shadow.getElementById('userInput');
        this.messageInput = this.shadow.getElementById('messageInput');
        this.sendButton = this.shadow.getElementById('sendButton');
        this.messagesList = this.shadow.getElementById('messagesList');

        // Add event listener to send button
        this.sendButton.addEventListener('click', this.sendMessage.bind(this));
    }

    connectedCallback() {
        // Initialize SignalR connection
        this.connection = new signalR.HubConnectionBuilder().withUrl("/gameHub").build();
        this.connection.start().then(() => {
            this.sendButton.disabled = false;
        }).catch((err) => {
            console.error(err.toString());
        });

        // Listen for incoming messages
        this.connection.on("ReceiveMessage", this.receiveMessage.bind(this));
    }

    sendMessage() {
        const user = this.userInput.value;
        const message = this.messageInput.value;
        this.connection.invoke("SendMessage", user, message).catch((err) => {
            console.error(err.toString());
        });
    }

    receiveMessage(user, message) {
        const li = document.createElement("li");
        li.textContent = `${user} says ${message}`;
        this.messagesList.appendChild(li);
    }
}

customElements.define('chat-component', ChatComponent);
