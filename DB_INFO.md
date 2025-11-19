# Faydark Admin Tool – Database Info

This file documents the current database setup for the Faydark Admin Tool (FAT).

---

## 1. Host & Connection Details

**Environment:** Production / Dev (shared for now)  
**Provider:** Hostinger (shared hosting)  
**Database Type:** MySQL

**Host (MySQL server):**
- `srv1999.hstgr.io`  
- IP: `82.197.82.199`

**Database Name:**
- `u902447017_FaydarkDB`

**MySQL User:**
- `u902447017_kael`

**Password:**
- **NOT STORED IN REPO**  
- Set/reset via Hostinger hPanel → *Databases → MySQL Databases*.

> 🔐 **Important:**  
> Never commit the real DB password to GitHub.  
> Store credentials in a local config (e.g. `user://fat_settings.cfg`, local JSON, or environment variables) and `.gitignore` it.

**Example connection string (C# / MySqlConnector):**

```csharp
string connStr =
    "Server=srv1999.hstgr.io;Port=3306;Database=u902447017_FaydarkDB;Uid=u902447017_kael;Pwd=YOUR_PASSWORD_HERE;SslMode=Preferred;";
