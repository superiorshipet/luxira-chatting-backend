# Luxira Internal Chat System - Backend

A real-time, WhatsApp-like internal chat system designed for employee communication. This system enforces strict business rules, notably that all communication happens within admin-managed groups (no private 1-to-1 direct messaging exists).

## Tech Stack
* **Framework:** .NET 10
* **Architecture:** Clean Architecture + Repository Pattern
* **Database:** PostgreSQL (via Entity Framework Core)
* **Real-time:** SignalR WebSockets
* **Caching & Presence:** Redis (StackExchange.Redis)
* **Authentication:** JWT Bearer tokens
* **Security:** BCrypt password hashing

## Prerequisites
* [.NET 10 SDK](https://dotnet.microsoft.com/)
* PostgreSQL (Running locally or via Docker)
* Redis Server (Running locally or via Docker)

## Setup & Running Locally

1. **Clone & Restore**
   ```bash
   dotnet restore
   ```

2. **Configure AppSettings**
   Open `InternalChat.API/appsettings.json` and update the connection strings if your local instances differ:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Database=luxira_chat;Username=postgres;Password=postgres",
     "Redis": "localhost"
   }
   ```
   *Note: Ensure both PostgreSQL and Redis servers are running.*

3. **Apply Migrations & Seed Database**
   The application automatically runs migrations and seeds an initial system admin on startup. Just run the API project:
   ```bash
   dotnet run --project InternalChat.API
   ```

### Default Admin Credentials
When the application first starts, it seeds a default Admin account:
* **Phone Number:** `+1234567890`
* **Password:** `AdminPassword123!`

---

## API Documentation (REST)

Base URL: `http://localhost:<port>/api`

### 1. Authentication
* **`POST /api/Auth/login`**
  * **Description:** Authenticates a user and returns a JWT token.
  * **Body:** `{ "phoneNumber": "+1234567890", "password": "..." }`
  * **Response:** `{ "token": "...", "user": { ... } }`

### 2. Admin Management 
*Requires Admin JWT `[Authorize(Roles = "Admin")]`*

* **`POST /api/Admin/users`**
  * **Description:** Creates a new employee user account.
  * **Body:** `{ "phoneNumber": "...", "password": "...", "fullName": "..." }`
* **`POST /api/Admin/users/{userId}/block`**
  * **Description:** Blocks a user (suspends account).
  * **Query Parameter:** `?reason=optional_string`
* **`POST /api/Admin/users/{userId}/unblock`**
  * **Description:** Unblocks a user.
* **`POST /api/Admin/groups`**
  * **Description:** Creates a new group.
  * **Body:** `{ "name": "Team A", "imageUrl": "..." }`
* **`POST /api/Admin/groups/{groupId}/members`**
  * **Description:** Adds members to a group.
  * **Body:** `{ "userIds": ["guid1", "guid2"] }`
* **`DELETE /api/Admin/groups/{groupId}/members/{userId}`**
  * **Description:** Removes a user from a group.

### 3. Group Operations
*Requires Standard user JWT `[Authorize]`*

* **`GET /api/Group/my-groups`**
  * **Description:** Gets a list of groups the authenticated user is currently a member of.
* **`GET /api/Group/{groupId}/members`**
  * **Description:** Gets active members of a specific group.
* **`GET /api/Group/{groupId}/messages`**
  * **Description:** Gets paginated message history for a group.
  * **Query Parameters:** `?beforeCursor={timestamp}&take=50`
* **`POST /api/Group/{groupId}/members/me/mute`**
  * **Description:** Mutes or unmutes a group for the authenticated user.
  * **Body:** `{ "isMuted": true }`

### 4. Message Operations
*Requires Standard user JWT `[Authorize]`*

* **`GET /api/Message/{messageId}/history`**
  * **Description:** Retrieves the edit history of a specific message.

### 5. Attachments
*Requires Standard user JWT `[Authorize]`*

* **`POST /api/Attachment/upload`**
  * **Description:** Uploads a file to local storage (returns a URL to be used when sending a message).
  * **Form-Data:** `file` (IFormFile)

---

## Real-time Documentation (SignalR Hub)

**Hub Endpoint:** `ws://localhost:<port>/hubs/chat`

**Authentication:** Pass the JWT token via query string parameter `access_token` when connecting to the socket.
*Example:* `/hubs/chat?access_token=<YOUR_TOKEN>`

### Client-to-Server Methods (Invoke)
* **`SendMessage(Guid groupId, string? content, MessageType type, string? attachmentUrl, Guid? replyToMessageId)`**
  * Sends a message to a group.
* **`EditMessage(Guid messageId, string newContent)`**
  * Edits a previously sent message.
* **`MarkAsRead(Guid groupId, Guid messageId)`**
  * Dispatches a read receipt.
* **`ReactToMessage(Guid groupId, Guid messageId, string emoji)`**
  * Adds or toggles an emoji reaction.
* **`ForwardMessage(Guid messageId, IEnumerable<Guid> targetGroupIds)`**
  * Forwards a message to multiple groups.

### Server-to-Client Events (Listen)
* **`ReceiveMessage(MessageDto message)`**
  * Triggered when a new message is posted in a joined group.
* **`MessageEdited(MessageDto message)`**
  * Triggered when a message is edited.
* **`MessageRead(Guid groupId, Guid messageId, Guid userId)`**
  * Triggered when a member reads a message.
* **`MessageReacted(Guid groupId, Guid messageId, Guid userId, string emoji)`**
  * Triggered when a member reacts to a message.
* **`UserPresenceChanged(Guid userId, bool isOnline)`**
  * Triggered globally when any user's online status changes.
* **`UserBlocked()`**
  * Triggered exclusively to the blocked user when an admin suspends their account.
* **`RemovedFromGroup(Guid groupId)`**
  * Triggered exclusively to a user when an admin removes them from a group.
