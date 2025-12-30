# Gamer Gamma v1.0 Beta

**Gamer Gamma** is a modern, high-performance monitor control utility designed for gamers and power users. It provides precise, hardware-level control over your display's color profile, allowing you to fine-tune your visual experience beyond what's possible with standard monitor OSDs or basic driver settings.

## Inspiration
Gamer Gamma is the spiritual successor to the legendary **Gamma Panel**. As Gamma Panel has become increasingly unstable or non-functional on modern Windows builds (Windows 10/11), Gamer Gamma was built from the ground up to restore that lost functionality with a modern interface and advanced "Stabilization" logic.

## Key Features

### 1. Master Control & RGB Linking
- **Master Gamma**: A single slider to control overall screen response.
- **Contextual Linking**: Use the **Linked** mode to apply relative shifts to all channels while preserving your custom color offsets. Switch to **Red, Green, or Blue** modes for independent channel surgical precision.

### 2. Levels (Core Adjustments)
- **Brightness**: Overall display gain.
- **Contrast**: The ratio between light and dark (Default: **0.5**).
- **Saturation**: Vividity of colors.

### 3. Stabilizers (The Secret Sauce)
- **Black Stabilizer**: Performs a 90-degree "floor cut." It lifts the darkest pixels into visibility without washing out mid-tones—perfect for seeing enemies in dark corners.
- **White Stabilizer**: Limits the peak output (ceiling), preventing highlight clipping and eye strain in bright scenes.
- **Shadow / Highlight**: Specific curves targets for the extremes of the spectrum.
- **Mid-Tone**: Adjusts the "belly" of the gamma curve (Default: **0.5**).

### 4. Extras & Technicals
- **Darkness**: Adjusts the absolute black point of the display.
- **Hue**: Shifts the entire color spectrum.
- **Dither**: Helps reduce color banding on lower-quality panels or extreme gamma settings.

### 5. Profile & Hotkey System
- **Profile Management**: Save your perfect settings for Different games (e.g., "Dark Souls", "Valorant", "Movies").
- **Flexible Hotkeys**: Bind any combination (e.g., **Ctrl+Alt+1**, **Alt+F1**) to instantly switch profiles mid-game.
- **Export/Import**: Move your profiles between machines easily via JSON.

### 6. System Integration
- **Minimize to Tray**: Keep your screen clean by hiding the app in the system tray.
- **Lightbulb Control**: Control the app via the system tray icon (Right-click to Open/Exit).

## How to Install & Run

1. Download the latest release from the [GitHub Releases](https://github.com/YOUR_USERNAME/GamerGamma/releases) page.
2. Run `GamerGamma.exe`. No installation required.
3. (Optional) Right-click and "Run as Administrator" if your graphics driver restricts gamma access to standard users.

## How to Build (Developers)

- **Requirements**: .NET 8.0 SDK, Windows 10/11.
- **Build**:
  ```bash
  dotnet build GamerGamma.csproj
  ```

## Credits
Built with ❤️ for the gaming community.
Inspiration: **Gamma Panel** (Original by Tomaž Šolc).
Logic & Implementation: **omaxtr**
Development Assistant: **Antigravity AI**
