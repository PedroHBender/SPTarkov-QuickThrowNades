# QuickThrow for SPT 4.x
THIS IS AN REUPLOAD AND ILL DELETE IT IF THE CREATOR WANT IT TO BE 

still testing the mod, so might be plenty of bugs 


QuickThrow restores fast grenade throwing for SPT 4.x. Instead of equipping a
grenade in hand first, the mod starts Tarkov's quick grenade controller and
throws the selected grenade immediately.

This version was updated for SPT 4.0.13.

## Features

- Press the grenade key to throw the highest-priority grenade immediately.
- Hold the grenade key and use the mouse wheel to choose a grenade, then quick
  throw that selected grenade.
- Hold the cancel key to keep vanilla grenade behavior.
- Hold the low-throw key to force a short/low throw.
- Optional debug file logging through `debug.cfg`.

## Default Keybinds

| Action | Default |
| --- | --- |
| Cancel QuickThrow and use vanilla grenade behavior | `Left Shift` |
| Force low throw | `Left Control` |

The keybinds are configurable in the BepInEx config file generated after the
first launch.

## Installation

1. Download or build `QuickThrow.dll`.
2. Place it in:

   ```text
   BepInEx/plugins/QuickThrow/QuickThrow.dll
   ```

3. Launch SPT once so BepInEx can generate the config.

## Building

The project references DLLs from your local SPT install. Pass your SPT folder
through `GameDir`:

```powershell
dotnet build .\QuickThrow.csproj -c Debug /p:GameDir="C:\Path\To\SPT"
```

You can also set an environment variable:

```powershell
$env:SPT_GAME_DIR = "C:\Path\To\SPT"
dotnet build .\QuickThrow.csproj -c Debug
```

After a successful build, the project copies the plugin DLL into:

```text
../BepInEx/plugins/QuickThrow/
```

## How It Works

QuickThrow uses Harmony prefixes in three places:

- `Player.SetInHands(ThrowWeapItemClass, Callback<IHandsThrowController>)`
  catches the grenade selector flow used when holding `G` and selecting with
  the mouse wheel.
- `Class1725.TranslateCommand(ECommand)` catches the direct `G` press in SPT
  4.x before it reaches the grenade selector UI.
- `Player.BaseGrenadeHandsController.vmethod_1(...)` forces the `low` throw
  flag when the configured low-throw key is held.

The quick throw itself is started with:

```csharp
player.SetInHandsForQuickUse(throwWeap, new Callback<GInterface206>(...));
```

In SPT 4.0.13, the quick grenade controller returns `GInterface206`, not
`IHandsThrowController`, so the mod does not try to convert callbacks between
those two controller types.

## Debugging

The mod creates these files beside the DLL:

```text
debug.cfg
QuickThrow_log.txt
```

Set `debug.cfg` to `true` to enable the custom file logger. The normal BepInEx
log always records when QuickThrow starts a quick grenade throw.

## Notes

This mod patches obfuscated EFT/SPT classes. Class and method names may change
between SPT versions, so future updates may require checking `Assembly-CSharp`
again with ILSpy or a metadata reader.
