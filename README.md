# MatchMaking Service

A .NET 8-based matchmaking service that handles player queues and match generation using Redis. Designed to be simple, scalable, and containerized.

---

## 📄 Configuration


Make sure to set `"Host": "redis"` to match your Redis container name.

---

## 🐳 How to Run

### 1️⃣ Create a Docker network

```bash
docker network create matchmaking-net
```

### 2️⃣ Run Redis (Alpine)

```bash
docker run -d --network=matchmaking-net -p 6379:6379 --name redis redis:7-alpine 
```

Make sure the container name is **`redis`**, so the matchmaking service can locate it via Docker DNS.

### 3️⃣ Build & Run the service

```bash
docker build -t matchmaking-app .
```
```bash
docker run -d --network=matchmaking-net -p 8080:8080 \
  -e ASPNETCORE_URLS="http://0.0.0.0:8080" \
  --name matchmaking-app matchmaking-app
```

The service will now be available at:

```
http://127.0.0.1:8080
```

---

## 🗺️ Data Flow Diagram

Want to see the architecture in action? Here's a **totally professional** diagram

👉 [View the Data Flow Diagram](https://drive.google.com/file/d/1b-KTdWvfjH9nA6GyERnx6E6zjcwovpR_/view?usp=drive_link)

---

## 📝 Notes

- The service manages player queues and creates matches via Redis commands.
- Runs inside a custom Docker bridge network (`matchmaking-net`).
- Kestrel server listens on port `8080` inside the container.
- The app will seed the Redis with 10,000 users with random rating from 1-5000.

---

Happy matchmaking! 🎯
