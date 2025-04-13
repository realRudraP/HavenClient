# HavenClient - Ephemeral P2P Chat

A simple, ephemeral peer-to-peer chat application built with C# and WinUI 3. One user hosts a session, acting as a temporary server with a REST API, and other users connect as clients. All chat messages are stored in memory on the host and are lost when the host application closes.

## Features

*   **Host/Client Model:** One instance acts as the host/server.
*   **API Driven:** The host runs an embedded ASP.NET Core REST API.
*   **Client Connectivity:** Clients connect to the host using its IP address and port.
*   **Group Chat:** All connected clients (and the host) participate in the same chat room.
*   **Polling Updates:** Clients periodically poll the host API for new messages and user lists.
*   **Active User List:** Displays nicknames of currently connected users (based on recent activity).
*   **Ephemeral:** Chat history exists only in the host's memory.
*   **Local Time Display:** Timestamps are shown in the user's local time zone.

## Technology Stack

*   **UI:** WinUI 3 (Windows App SDK)
*   **Language:** C#
*   **.NET:** .NET 7.0 (or later)
*   **API Server (Host):** ASP.NET Core Web API (embedded)
*   **Client HTTP:** `System.Net.Http.HttpClient`
*   **Real-time (Simulated):** HTTP Polling

## How it Works

1.  **Host:** When a user chooses to host:
    *   The application starts an ASP.NET Core Kestrel server in the background.
    *   This server exposes a minimal REST API (e.g., `/api/message`, `/api/users`).
    *   The host's local IP address and the listening port (e.g., 5000) are displayed.
    *   A `MessageService` singleton manages the in-memory list of messages and active users.
    *   Messages sent by the host UI are added directly to the `MessageService`.
2.  **Client:** When a user chooses to join:
    *   They enter the IP address and port provided by the host.
    *   The client uses `HttpClient` to interact with the host's REST API.
    *   **Sending Messages:** The client sends a `POST` request to `/api/message` on the host.
    *   **Receiving Messages:** The client periodically sends a `GET` request to `/api/message?since={last_message_id}` to retrieve new messages.
    *   **Fetching Users:** The client periodically sends a `GET` request to `/api/users` to get the current list of participants.
3.  **Message Flow:**
    *   Client A sends message -> POST to Host API.
    *   Host API -> `MessageService` adds message to its list.
    *   Client B polls -> GET from Host API -> Receives message from Client A (and any others).
    *   Client A polls -> GET from Host API -> Receives its own message (confirming delivery) and messages from others.


### Running

**1. Start the Host:**

*   Run the built application (`HavenClient.exe`).
*   On the initial screen (or wherever the choice is made), enter a nickname.
*   Click the "Host" button.
*   The chat page will load. Note the IP address and port displayed (e.g., `Hosting. Others connect to: 192.168.1.10:5000`). You need to provide this exact address:port combination to others who want to join.

**2. Start a Client:**

*   Run *another instance* of the built application (`HavenClient.exe`).
*   Enter a unique nickname.
*   Enter the **exact**, with the http:// protocol IP address and port shown by the host instance (e.g., `http://192.168.1.10:5000`).
*   Click the "Join" button.
*   The chat page will load, connected to the host.

**3. Chat!**

*   Messages sent by any client or the host will appear in all connected instances after a short polling delay.
*   The active user list will update periodically.

## Known Issues / Limitations

*   **Polling Delay:** Messages appear after the polling interval (currently ~3 seconds), not instantly.
*   **No Persistence:** Chat history is lost when the host instance is closed.
*   **Basic Error Handling:** Connection errors might not be handled gracefully. The client might stop polling if the host becomes unreachable.
*   **No Encryption:** Communication is plain HTTP. Do not use for sensitive information.
*   **Simple Discovery:** Requires manual sharing of the host's IP address and port.
*   **User List Timeout:** Users are removed from the list after a period of inactivity (currently 5 minutes without sending a message).

## Future Enhancements (TODO)

*   Implement real-time communication (WebSockets or SignalR) instead of polling.
*   Add message persistence (e.g., saving to a local file on the host).
*   Implement end-to-end encryption.
*   Add support for file sharing.
*   Improve UI/UX.
*   More robust error handling and connection status reporting.
*   Consider NAT traversal solutions for connections outside the local network (more complex).

