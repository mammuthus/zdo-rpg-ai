# Zdo RPG AI

System to make NPCs in the RPG alive.

![Screenshot](./doc/screenshot.jpg)

This is evolution of https://github.com/drzdo/immersive_morrowind_llm_ai

Right now this mod is primarily focused on Morrowind. But I would like to keep it extandable for other games as well.

# Instructions

Server:

```sh
dotnet run --project src/ZdoRpgAi.Server.Console -- --config .tmp/server-config.yaml
```

Client:

```sh
dotnet run --project src/ZdoRpgAi.Client.Console -- --config .tmp/client-config.yaml
```

Game mods:

- Morrowind OpenMW https://github.com/drzdo/zdo-rpg-ai-openmw-mod
