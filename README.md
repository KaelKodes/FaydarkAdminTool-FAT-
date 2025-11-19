
# 📘 Faydark Admin Tool (FAT)

FAT (Faydark Admin Tool) is the official world-building and administrative tool used to manage the MMO environment of Faydark — a gothic/cosmic-fantasy RPG universe.
This tool is used internally by the development team to create, edit, and maintain all server-side data that powers the Faydark world.

Players never interact with FAT.
FAT is strictly for developers, designers, and live-ops administrators.

# 🌌 What Is Faydark?

Faydark is not just a forest — it’s the creeping eldritch mist devouring worlds.
Your Refuge stands as the final human sanctuary, and FAT is how we shape this world.

FAT gives us the power to build:

🌍 MMO worlds

🌿 Biomes

🧱 Tiles & procedural maps

🧙‍♂️ Classes, races, NPCs 

🏰 Towns & structures

📦 Items & loot

⚙️ Server-side systems


# 🛠️ Tech Stack

Engine: Godot 4.5.1 (Mono / C#)

Language: C# only

Database: MySQL (Hostinger)

ORM/Driver: MySqlConnector (.NET library)

UI: Godot Control system

FAT is fully C#, except for trivial GDScript UI helpers (rare).


# 🤝 Contributing

Because this is a private internal tool, contributions are limited to the Faydark dev team.

Follow these standards:

C# only

No magic numbers

No speculative code — check existing systems first

Use DBManager for all DB interactions

Maintain FAT’s modular structure

UI changes should be incremental

📜 License

Internal project — not licensed for public distribution.
