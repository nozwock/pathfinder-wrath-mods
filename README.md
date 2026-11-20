# Pathfinder Wrath of the Righteous Mods

## Installation

1. Install [UMM].
2. Ensure the game loads only UMM's `0Harmony` by doing one of the following:
    1. Delete or rename `<Game Folder>/Wrath_Data/Managed/0Harmony.dll`.
    2. Set the following in `<Game Folder>/doorstop_config.ini`:
    ```ini
    [UnityMono]
    dll_search_path_override=Wrath_Data/Managed/UnityModManager
    ```
3. Extract the contents of mod archives into `<Game Folder>/Mods/`.

[umm]: https://www.nexusmods.com/site/mods/21
