# Desktop Pet (WPF, .NET 9)

A tiny transparent desktop pet for Windows. It sits on your screen, animates a GIF, and can fetch a random cat GIF from TheCatAPI on demand.

## Features

- Animated GIF rendering using WpfAnimatedGif (no flicker, true animation)
- Transparent, frameless, always-on-top window
- Drag anywhere to move the pet
- Right-click context menu:
	- New Random Cat 🐱 — fetches a random cat GIF from TheCatAPI
	- Exit — closes the app

## Requirements

- Windows 10/11
- .NET SDK 9 (or later) installed

## Build and run

From a terminal, build and run the WPF app:

```powershell
cd .\DesktopPet
dotnet build
dotnet run
```

The pet window will appear near the bottom of your main display. Drag to reposition.

## Usage

- Right-click the pet and choose “New Random Cat 🐱” to switch to a random cat GIF.
- Choose “Exit” to quit.

Tip: If you click “New Random Cat 🐱” very quickly multiple times, the app will queue/ignore overlapping requests so it stays stable.

## How it works (short)

- The initial `cat.gif` is bundled as a WPF Resource and displayed with `WpfAnimatedGif`.
- When you request a random cat:
	1) The app calls `https://api.thecatapi.com/v1/images/search?mime_types=gif&limit=1`.
	2) It downloads the GIF bytes, then switches the animated image on the UI thread.
	3) It keeps the GIF stream alive while animating and disposes the previous one after a short delay. This avoids known edge cases in WPF’s image pipeline when changing images quickly.

No API key is required for basic usage of TheCatAPI for GIF search.

## Troubleshooting

- A dialog says: “Something went wrong, but your pet is okay. We’ll keep the current image.”
	- This means an unexpected UI exception occurred; the app catches it so it won’t close. Click “Open Log” and check the last lines for details.
- The GIF doesn’t animate (looks like a still image):
	- Ensure the WpfAnimatedGif package is installed and the XAML uses `gif:ImageBehavior.AnimatedSource`.
- “New Random Cat” appears not to change the image:
	- Sometimes the API returns the same cat. Click again after a second or two.
- Where’s the log?
	- The app writes `DesktopPet.log` in `DesktopPet/bin/Debug/net9.0-windows/DesktopPet.log`.

## Project layout

- `DesktopPet/MainWindow.xaml` — UI layout (transparent window, Image, context menu)
- `DesktopPet/MainWindow.xaml.cs` — behavior (dragging, random cat loader, logging hooks)
- `DesktopPet/App.xaml` and `App.xaml.cs` — app startup and global exception handling
- `DesktopPet/DesktopPet.csproj` — project configuration, WPF enabled, package references

## Dependencies

- [WpfAnimatedGif](https://github.com/thomaslevesque/WpfAnimatedGif) — animated GIF support in WPF
- [Newtonsoft.Json](https://www.newtonsoft.com/json) — simple JSON parsing

## Notes

- The window is always on top and doesn’t show in the taskbar by design.
- If you want to start with a random cat automatically at launch, it’s easy to call the loader from `MainWindow` after `InitializeComponent()`.

---
Enjoy your desktop cat! 🐱
