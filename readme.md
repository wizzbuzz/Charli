# Charli

Charli is a Windows Forms utility that provides an overlay UI to easily add diacritics.  
It runs in the system tray and uses low-level keyboard hooks to detect custom hotkey presses and display an overlay interface.

---

## Features

- Global hotkey support with customizable modifiers and main key
- System tray icon with context menu (Exit)
- Overlay window that appears on hotkey press
- Diacritical character input support
- Runs minimized and hidden from the taskbar by default
- Lightweight and minimal UI

---

## Getting Started

### Prerequisites

- .NET 9.0 SDK or newer
- Windows OS (because of Windows API keyboard hooks)
- A code editor like Visual Studio Code or Visual Studio

### Build and Run

1. Clone the repository:

   ```bash
   git clone https://github.com/yourusername/OverlayApp.git
   cd OverlayApp
   Restore dependencies and build the project:
   ```

bash
Copy
Edit
dotnet build
Run the application:

bash
Copy
Edit
dotnet run --project OverlayApp.csproj
Usage
The app runs in the system tray with the icon defined by favicon.ico.

The default hotkey is Ctrl + Shift + A, which you can customize in the settings window.

Double-click the tray icon to show the settings window.

Change your hotkey modifiers and key, then click Save.

Press your hotkey to toggle the overlay window.

To exit the app, right-click the tray icon and choose Exit.

Contributing
Contributions are welcome!
Feel free to open issues or submit pull requests for bug fixes and new features.

License
This project is licensed under the MIT License. See the LICENSE file for details.

Contact
Created by Joop\_
Feel free to reach out if you have questions or feedback.
