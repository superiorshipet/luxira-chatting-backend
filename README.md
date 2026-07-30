# Luxira Chatting Backend - Complete API Documentation

Welcome to the Luxira Chatting Backend project! This repository contains the ASP.NET Core Web API powering the real-time chat, video, and voice calling system for Luxira Holding.

---

## 📡 SignalR Hub (`/hubs/chat`)

The application utilizes SignalR for real-time messaging, presence, and WebRTC signaling.
**Connection URL:** `ws://<domain>/hubs/chat?access_token=<JWT_TOKEN>`

### Client-to-Server Methods (Invoke these from frontend)
- `SendMessage(Guid groupId, string? content, MessageType type, string? attachmentUrl, Guid? replyToMessageId)`
- `EditMessage(Guid messageId, string newContent)`
- `DeleteMessage(Guid groupId, Guid messageId)`
- `PinMessage(Guid groupId, Guid messageId, bool isPinned)`
- `ReactToMessage(Guid groupId, Guid messageId, string emoji)`
- `MarkAsRead(Guid groupId, Guid messageId)`
- `ForwardMessage(Guid messageId, IEnumerable<Guid> targetGroupIds)`
- `UserTyping(Guid groupId, bool isTyping)`
- `JoinGroup(Guid groupId)` / `LeaveGroup(Guid groupId)`

**WebRTC Signaling:**
- `CallOffer(Guid targetGroupId, string sdpOffer, bool isVideo, Guid? targetUserId = null)`
- `CallAnswer(Guid targetGroupId, Guid targetUserId, string sdpAnswer)`
- `SendIceCandidate(Guid targetGroupId, Guid? targetUserId, string candidate)`
- `EndCall(Guid targetGroupId)`

### Server-to-Client Events (Listen to these on frontend)
- `ReceiveMessage(MessageDto message)`
- `MessageEdited(MessageDto message)`
- `MessageDeleted(Guid groupId, Guid messageId, Guid userId)`
- `MessagePinned(Guid groupId, Guid messageId, bool isPinned)`
- `MessageReacted(Guid groupId, Guid messageId, Guid userId, string emoji)`
- `MessageRead(Guid groupId, Guid messageId, Guid userId)`
- `UserTyping(Guid groupId, Guid userId, bool isTyping)`
- `UserPresenceChanged(Guid userId, bool isOnline)`
- `IncomingCall(Guid callerId, string sdpOffer, bool isVideo, Guid groupId)`
- `CallAnswered(Guid userId, string sdpAnswer)`
- `IceCandidate(Guid userId, string candidate)`
- `CallEnded(Guid userId)`

---

## 🌐 REST API Endpoints

All endpoints are prefixed with `/api`.
Many endpoints require an `Authorization: Bearer <token>` header, indicated by **[Auth]**. Admin-only endpoints require the user to have the Admin role, indicated by **[Admin]**.

---

### 🔐 Authentication (`/api/Auth`)

#### `POST /api/Auth/login`
- **Description:** Login with phone number and password.
- **Request Body:**
  ```json
  {
    "phoneNumber": "+20123456789",
    "password": "Password123!"
  }
  ```
- **Response:**
  ```json
  {
    "token": "eyJhbGciOiJIUzI1...",
    "user": {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "phoneNumber": "+20123456789",
      "fullName": "John Doe",
      "profileImageUrl": "https://...",
      "role": 0, // 0 = Admin, 1 = Employee
      "status": 0,
      "isOnline": true,
      "lastSeenAt": "2023-01-01T00:00:00Z",
      "isVerified": true,
      "canReceivePrivateMessages": true
    }
  }
  ```

#### `POST /api/Auth/forgot-password`
- **Description:** Request a password-reset token (sent to the user's email).
- **Request Body:**
  ```json
  {
    "phoneNumber": "+20123456789",
    "email": "employee@example.com"
  }
  ```

#### `POST /api/Auth/reset-password`
- **Description:** Reset password using the token received by email.
- **Request Body:**
  ```json
  {
    "email": "employee@example.com",
    "token": "123456",
    "newPassword": "NewPassword123!"
  }
  ```

#### `GET /api/Auth/me` **[Auth]**
- **Description:** Get the profile of the currently authenticated user.
- **Response:** Returns `UserDto` (same as in Login response).

#### `PUT /api/Auth/profile` **[Auth]**
- **Description:** Update the authenticated user's own profile.
- **Request Body:**
  ```json
  {
    "fullName": "Updated Name",
    "profileImageUrl": "https://..."
  }
  ```

---

### 👥 Users (`/api/Users`)

#### `GET /api/Users/{userId}/profile` **[Auth]**
- **Description:** Get the public profile of any user. Phone numbers are hidden. Includes media shared in common groups.
- **Response:**
  ```json
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "fullName": "John Doe",
    "profileImageUrl": "https://...",
    "isVerified": true,
    "isOnline": true,
    "lastSeenAt": "2023-01-01T00:00:00Z",
    "sharedMedia": [
      {
        "messageId": "3fa85f64...",
        "url": "https://...",
        "fileType": "image/jpeg",
        "sentAt": "2023-01-01T00:00:00Z"
      }
    ]
  }
  ```

#### `POST /api/Users/favorites/{groupId}` **[Auth]**
- **Description:** Toggle a specific group chat as favorite/unfavorite for the current user.

---

### 🛠️ Admin Management (`/api/Admin`) **[Admin Only]**

#### `GET /api/Admin/users`
- **Description:** List all employees in the system. Returns a list of `UserDto`.

#### `POST /api/Admin/users`
- **Description:** Create a new employee account.
- **Request Body:**
  ```json
  {
    "phoneNumber": "+20123456789",
    "password": "Password123!",
    "fullName": "New Employee",
    "email": "new@example.com"
  }
  ```

#### `POST /api/Admin/users/{userId}/block`
- **Description:** Block an employee.
- **Request Body:** `{ "reason": "Violation of policy" }`

#### `POST /api/Admin/users/{userId}/unblock`
- **Description:** Unblock an employee.

#### `POST /api/Admin/users/{userId}/verify`
- **Description:** Toggle the verification badge (blue tick) for a user.

#### `PUT /api/Admin/users/{userId}/private-permission`
- **Description:** Grant or revoke permission for an employee to send private messages to the admin.
- **Request Body:** `{ "grant": true }`

#### `GET /api/Admin/groups`
- **Description:** Get all non-private groups in the system. Returns a list of `GroupDto`.

#### `POST /api/Admin/groups`
- **Description:** Create a new public group chat.
- **Request Body:**
  ```json
  {
    "name": "General Chat",
    "imageUrl": "https://..."
  }
  ```

#### `POST /api/Admin/groups/private`
- **Description:** Create a private 1-on-1 chat with a specific employee.
- **Request Body:** `{ "targetUserId": "3fa85f64..." }`

#### `PUT /api/Admin/groups/{groupId}`
- **Description:** Update group details (name, image).
- **Request Body:**
  ```json
  {
    "name": "Updated Group Name",
    "imageUrl": "https://..."
  }
  ```

#### `POST /api/Admin/groups/{groupId}/members`
- **Description:** Add members to a group.
- **Request Body:** `{ "userIds": ["3fa85f64...", "1fa85f64..."] }`

#### `DELETE /api/Admin/groups/{groupId}/members/{userId}`
- **Description:** Remove a member from a group.

#### `PUT /api/Admin/groups/{groupId}/members/{userId}/mute`
- **Description:** Mute/unmute a member in a group (also restricts replying in private chats).
- **Request Body:** `{ "isMuted": true }`

---

### 💬 Groups & Chats (`/api/Group`)

#### `GET /api/Group/my-groups` **[Auth]**
- **Description:** Get all groups the user belongs to.
- **Query Parameters:** `?filter=unread` or `?filter=favorites` (optional)
- **Response:** List of `GroupDto`.
  ```json
  [
    {
      "id": "3fa85f64...",
      "name": "Design Team",
      "imageUrl": "https://...",
      "createdAt": "2023-01-01T00:00:00Z",
      "isPrivate": false,
      "privateTargetUserId": null,
      "isFavorite": true,
      "unreadCount": 5,
      "lastMessage": "Here is the new design",
      "lastMessageAt": "2023-01-01T00:00:00Z"
    }
  ]
  ```

#### `GET /api/Group/{groupId}/members` **[Auth]**
- **Description:** Get the members of a specific group. Returns a list of `GroupMemberDto`.
  ```json
  [
    {
      "userId": "3fa85f64...",
      "fullName": "John Doe",
      "profileImageUrl": "https://...",
      "role": "Admin",
      "isOnline": true,
      "lastSeenAt": "2023-01-01T00:00:00Z",
      "isMuted": false,
      "isVerified": true
    }
  ]
  ```

#### `GET /api/Group/{groupId}/messages` **[Auth]**
- **Description:** Get paginated message history for a group.
- **Query Parameters:** `?beforeCursor=2023-01-01T00:00:00Z` & `?take=50`
- **Response:** List of `MessageDto`.
  ```json
  [
    {
      "id": "3fa85f64...",
      "groupId": "3fa85f64...",
      "senderId": "3fa85f64...",
      "senderName": "John Doe",
      "content": "Hello World",
      "messageType": 0, // 0 = Text, 1 = File, 2 = Voice
      "sentAt": "2023-01-01T00:00:00Z",
      "isEdited": false,
      "isDeleted": false,
      "isPinned": false,
      "replyToMessageId": null,
      "forwardedFromMessageId": null,
      "forwardedFromGroupId": null,
      "attachments": [
        {
          "fileUrl": "https://...",
          "fileType": "image/jpeg",
          "fileSizeBytes": 102400,
          "thumbnailUrl": null,
          "durationSeconds": null
        }
      ]
    }
  ]
  ```

#### `GET /api/Group/search` **[Auth]**
- **Description:** Search for a keyword across all messages in groups the user belongs to.
- **Query Parameters:** `?keyword=hello`

#### `POST /api/Group/{groupId}/mark-read` **[Auth]**
- **Description:** Mark all messages in a specific group as read for the current user.

---

### ✉️ Messages (`/api/Message`)

#### `GET /api/Message/{messageId}/history` **[Auth]**
- **Description:** Get the edit history (previous versions) for a specific message.
- **Response:** List of `MessageEditHistoryDto`.
  ```json
  [
    {
      "messageId": "3fa85f64...",
      "oldContent": "Helllo",
      "editedAt": "2023-01-01T00:00:00Z"
    }
  ]
  ```

#### `POST /api/Message/{messageId}/favorite` **[Auth]**
- **Description:** Toggle a specific message as a favorite/unfavorite for the current user.

#### `GET /api/Message/favorites` **[Auth]**
- **Description:** Get all favorited messages of the current user. Returns a list of `MessageDto`.

---

### 📁 Files & Media Uploads (`/api/Files` & `/api/Attachment`)

#### `POST /api/Files/upload` **[Auth]**
- **Description:** Upload any file (image, video, audio, document) to Cloudinary. Max size: 50MB.
- **Request Body:** Form-Data containing `file` (IFormFile).
- **Response:**
  ```json
  {
    "url": "https://res.cloudinary.com/...",
    "fileName": "document.pdf",
    "size": 102400,
    "fileType": "application/pdf"
  }
  ```

#### `POST /api/Files/upload/profile` **[Auth]**
- **Description:** Upload a profile picture. Images only (jpg, png, webp, gif). Max size: 10MB.
- **Request Body:** Form-Data containing `file` (IFormFile).
- **Response:** `{ "url": "https://res.cloudinary.com/..." }`

#### `POST /api/Attachment/upload` **[Auth]**
- **Description:** General file/attachment upload endpoint (same functionality as `/api/Files/upload`).
