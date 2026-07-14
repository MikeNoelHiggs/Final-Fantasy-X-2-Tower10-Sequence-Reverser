# FFX2 Tower 10 Sequence Reverser

## Introduction
FFX2 Tower 10 Sequence Reverser is a .NET for Android utility app that helps with the Tower 10 input flow in *Final Fantasy X-2*. The app captures PS5 controller inputs and displays them in reverse order so the final left-to-right sequence is easier to follow during each turn.

## Getting Started
### 1) Installation process
1. Clone the repository.
2. Ensure .NET 10 and the Android workload are installed.
3. Build and deploy to an Android device (API level 24+).
4. Pair a PS5 DualSense controller over Bluetooth.

### 2) Software dependencies
- .NET SDK 10 with Android workload
- Physical Android device
- Visual Studio Community 2026 (or equivalent .NET Android tooling)

### 3) Latest releases
No tagged releases are currently published. Use the `Init` branch as the active baseline.

### 4) API references
- [.NET for Android documentation](https://learn.microsoft.com/dotnet/maui/android/)
- [Android KeyEvent reference](https://developer.android.com/reference/android/view/KeyEvent)
- [Android MotionEvent reference](https://developer.android.com/reference/android/view/MotionEvent)

## Build and Test
### Build
From the solution root:

```powershell
dotnet restore
dotnet build .\FFX2Tower10SequenceReverser.slnx -c Debug
```

### Run
- Deploy from Visual Studio to a connected Android device.
- Start the app and connect a PS5 controller.
- Use **Options** for next turn and **Share** to reset.

### Test
There are no automated test projects in the solution yet. Validation is currently manual through on-device controller input checks.

## Contribute
Contributions are welcome.

1. Create a branch from `Init`.
2. Keep changes focused and consistent with the current coding style.
3. Verify the app builds and behaves correctly on Android.
4. Open a pull request with a concise summary and validation notes.

For larger changes, include rationale and screenshots/GIFs for UI or controller behavior updates.
