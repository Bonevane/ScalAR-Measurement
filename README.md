# ScalAR Measurement App

An augmented reality measurement application built with Unity and Google ARCore that allows users to measure real-world distances using their Android device's camera.

## 📱 Features

- **Real-time AR Measurement**: Place points in 3D space and measure distances between them
- **Visual Preview**: Interactive preview sphere that shows where measurements will be placed
- **Snapping Functionality**: Automatic snapping to nearby points for precise measurements
- **Multiple Measurements**: Support for multiple simultaneous measurements with visual line indicators
- **Depth Integration**: Utilizes ARCore's depth API for enhanced precision
- **Haptic Feedback**: Vibration feedback when snapping to points
- **Billboard Text**: Distance labels that always face the camera for easy reading
- **Reset Functionality**: Clear all measurements with a single button

## 🛠️ Technical Specifications

- **Unity Version**: 6000.1.15f1
- **Target Platform**: Android
- **AR Framework**: Google ARCore 6.1.1
- **Rendering Pipeline**: Universal Render Pipeline (URP) 17.1.0
- **Input System**: Unity Input System 1.14.0
- **Minimum Android Version**: ARCore compatible devices

## 📋 Requirements

### Device Requirements

- Android device with ARCore support
- Android API level 24 (Android 7.0) or higher
- Sufficient device storage and RAM

### Development Requirements

- Unity 6000.1.15f1 or compatible version
- Android SDK and NDK
- ARCore SDK for Unity
- Visual Studio or compatible IDE

## 📦 Key Dependencies

| Package          | Version | Purpose                   |
| ---------------- | ------- | ------------------------- |
| AR Foundation    | 6.1.1   | Core AR functionality     |
| ARCore XR Plugin | 6.1.1   | Google ARCore integration |
| XR Core Utils    | 2.5.2   | XR utilities and helpers  |
| Universal RP     | 17.1.0  | Rendering pipeline        |
| Input System     | 1.14.0  | Modern input handling     |
| TextMeshPro      | 3.2.0+  | UI text rendering         |

## 🚀 Getting Started

You can download the APK available in the repository or build it yourself by following these steps:

1. **Clone the Repository**

   ```bash
   git clone https://github.com/Bonevane/ScalAR-Measurement.git
   cd ScalAR-Measurement
   ```

2. **Open in Unity**

   - Launch Unity Hub
   - Open the project folder
   - Ensure Unity 6000.1.15f1 is installed

3. **Configure Build Settings**

   - Go to File → Build Settings
   - Select Android platform
   - Configure XR settings for ARCore

4. **Build and Deploy**
   - Connect your ARCore-compatible Android device
   - Build and run the project

## 🎮 How to Use

1. **Launch the App**: Start the application on your ARCore-compatible device (Permissions Required)
2. **Point Camera**: Aim your device at a flat surface to initialize AR tracking
3. **Place Points**: Tap the screen or use the place button to set measurement points
4. **View Distance**: The distance between points will be displayed automatically
5. **Multiple Measurements**: Continue placing points to create multiple measurements
6. **Reset**: Use the reset button to clear all measurements and start over

## 🏗️ Project Structure

```
Assets/
├── Animations/          # Animation files and controllers
├── Fonts/              # Custom fonts for UI
├── Materials/          # Material assets for 3D objects
├── Prefabs/            # Reusable game objects
├── Resources/          # Runtime loadable assets
├── Scenes/             # Unity scene files
│   ├── Startup.unity   # Initial loading scene
│   └── ARScene.unity   # Main AR measurement scene
├── Scripts/            # C# scripts
│   └── ARTapMeasure.cs # Main measurement logic
├── Settings/           # Project and render pipeline settings
└── XR/                 # XR-specific assets and configurations
```

## ⚠️ Known Issues & Limitations

### Current Known Issues

- **Jittery Placement Pointer**: The placement preview sphere can exhibit minor jittering, especially in low lit environments
- **Surface Rotation**: The placement pointer does not visually rotate to match slanted or angled surfaces

### General Limitations

- **Lighting Dependency**: Performance may vary in low-light conditions
- **Surface Requirements**: Works best on well-textured, non-reflective surfaces
- **Tracking Loss**: May lose tracking in environments with insufficient visual features
- **Accuracy**: Measurement accuracy depends on ARCore's plane detection and device calibration
- **Battery Usage**: Extended use may drain battery due to intensive camera and processing usage


## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

Contributions are greaty appreciated!

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Google ARCore team for the AR framework
- Unity Technologies for the game engine
- Unity AR Foundation team for AR development tools

## 📞 Support

For issues, questions, or contributions, please:

- Open an issue on GitHub
- Check ARCore documentation for device-specific issues

---

**Note**: This application requires an ARCore-compatible device. Check Google's official ARCore device list to ensure compatibility before installation.
