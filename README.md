# VR Playground

This project was developed as part of a student project at the HfM (Trossingen), featuring a musical and interactive virtual world where players can collaborate, interact with objects, and react to their environment. Its starting ground was a Multiplayer Template by Maximilian Flack and Noam Hartmann. 

Developed by Valentin Händler and supervised by Prof. Norbert Schnell  

---

## Disclaimer

- **Asset-Use:** This Project uses Free Assets made by SpaceZeta ("Spotlight and Structure"), the Unity XR Assets and the "FMOD for Unity" Asset. Because this project is made without the intent of earning money and its budget was zero dollars, its use case can be interpreted as "free-use". Please keep that in mind, when downloading this repository. Music and Sound was made by Valentin Händler. When its content is being further puplished a confirmation is requested. 
- **Development-Status:** The project is provided "as-is." No guarantee is given regarding compatibility with all VR headsets. Check the release page for further compatibility information
- **AI-Use:** This projects code has been made with the help of AI, especially its multiplayer features. But all assets are made by a human.  

---

## Features

![Hand Tracking Demo](docs/Gameplay.gif) 

- **Interactive Music:** Use different balls for different sounds to generate music! The change of there X/Y/Z-Coordinates, changes the Reverb, the Detune or the EQ.
- **Hand Tracking & Gesture Support:** The world is fully interactable with the Controls of the VR-Headset 
- **Interactable Menu:** Find a menu with sliders and new controls, including a reset function for the balls positions and a volume slider of a delay effect  
- **Distance effected Music:** The distance of the players to each other trigger a looped ambiant track that gets clearer the closer each player gets to. 
- **Standalone Multiplayer Functionality:** The features of multiplayer can be activated with a standalone pico and no needed extra servers. The headset acts as the server  

---

## How to clone the Repository

### Prerequisites:

Please make sure you have all needed versions installed:

* **Unity Editor:** Version `6000.1.17f1` (newer versions may have code conflicts)
* **Unity Modules:** Android Build Support 
* **Assets:** "FMOD for Unity"; "XR Interaction Toolkit"; "Universal Render Pipeline"

### Installation & Setup

1. **Clone Repository:**
   ```bash
   git clone https://https://github.com/ValHaen/VR_Playground.git
   ```
   - Or download the source code as a zip file
2. **Open the project in Unity Hub:**
   - Open the **Unity Hub**.
   - Press on **Add** -> **Add project from disk**.
   - Choose the cloned repository and choose the correct Unity Version
3. **Start the project:**
   - The project will probably start in `Safe Mode`, because of the missing assets
   - Navigate to **Window** -> **Package Manager** 
   - Install all named Assets in the `Prerequisites` section 
   - When play testing make sure to start in the "StartupScene", because there all VR Assets will be loaded

---

### How to Download the APK

1. **Install the newest version:**
   - Check the [Release](https://github.com/ValHaen/VR_Playground/releases) page for the newest APK and download it
2. **Install the APK on the Pico Headset:**
   - Make sure you unlocked the "Developer" Mode on your Pico Headset. Without the Headset will block unknown APK's
   - One way of transferring the APK File on your Pico Headset is through the "Pico Developer Center" App. Although a Pico Developer Account is required.
   - Another way is through the ADB Services (from Android SDK Platform Tools). Please use at your own risk
3. **Open the APK File:**
   - Inside the headset navigate to your downloaded file and open it.

## !!Disclaimer!!:
There is a known bug of a needed Pico Account to start the file. Im currently working on fixing that. Right now your headsets needs to be logged in (for some reason).

---

###  Licence

This project is puplished under the **MIT-Licence** – see the [LICENSE](LICENSE)-File for details