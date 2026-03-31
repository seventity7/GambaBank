# `Changelog 1.0.3 - 03/31/2026`

## ✨ Added

- 🎭 **Dual Mode system**
  - Added separate **Dealer** and **Player** modes
  - Each mode now has its own workflow and dedicated history behavior

- 🏠 **House field**
  - Added a new **House** input field
  - Saved to history for better session organization

- 🔎 **History search**
  - Added a **Search** field to quickly find entries in history
  - Supports searching across saved session information

- 🤖 **Auto Track system (Player Mode)**
  - Added **Auto Track** toggle for automated player result tracking
  - Can track a selected dealer and react to party chat results automatically

📊 History System
_Stores full session data:_
- Date
- Time
- Starting Bank
- Final Bank
- Tips
- Results

- 🎯 **Dealer tracking**
  - Added **Track Dealer** support based on the current target
  - Allows selecting a specific dealer to monitor instead of reading unrelated chat

- 📊 **Player session auto-history**
  - Player Mode now automatically saves entries to history when tracked results happen
  - Supports:
    - Wins
    - Losses
    - Busts
    - Pushes
    - Banking

- 🧮 **Repeated result detection**
  - Auto Track supports cases where the same player name appears multiple times in one result line
  - Correctly multiplies gains/losses based on how many times the player appears

- 💰 **Banking tools**
  - Added quick banking increment buttons:
    - `+50K`
    - `+100K`
    - `+250K`
    - `+500K`
    - `+1M`
  - Added manual banking amount field
  - Added **Add Bank** button to add banking directly to Current Bank

## 🛠️ Fixed

- 🧷 Fixed multiple Player Mode layout and alignment issues
- 🧯 Fixed clipping/cropping issues affecting controls in the tracking/banking area
- 🧹 Fixed compile issues related to helper methods and Dalamud namespace usage

## 🎨 UI Overhaul + Quality of Life

- 🚦 Added status feedback for tracking:
  - **ON / OFF** tracking state
  - Temporary round-result feedback such as:
    - `You won!`
    - `You Lost!`
    - `You Busted!`
    - `Bet Pushed!`

- ⛔ Added protection states for bet input:
  - **Need Start Bank** when no starting bank is set
  - **Not enough bank** when Current Bank is too low for the current bet

## 📝 Improved

- 📚 Dealer and Player workflows are much more distinct and easier to use
- 🗂️ Player History have better reflects round-by-round results
- 📈 History entries accurately represent:
  - Start Bank
  - Total Bank
  - Result outcome
  - House
  - Timestamp

- ⚙️ Auto tracking logic is now more robust for real blackjack hosting scenarios where result messages may contain:
  - multiple player names
  - repeated names
  - mixed result categories in a single line

## 🧾 Notes

- **Player Mode** is designed for automated round tracking and quick personal bankroll management, meant for as you are playing the game
- **Dealer Mode** remains focused on manual session/result tracking, meant for as you are Dealing the game
- Auto Track only reacts to the tracked dealer’s party chat messages
- Push results do not modify Current Bank, but are still saved to history properly

`End of changelog`


# **💰 Gamba Bank**

- Designed to help players track their gil gains and losses in a simple and clean way.
- It provides in-game calculator that formats values automatically and generates a ready-to-copy message for sharing results.

## **✨ Features**

- 🔸 Dealer Mode: Track Start Bank, Final Bank, Tips, Day results, All time profits/losses
- 🔸 Player mode: Real-time tracking of betting, losses, wins and banking
 - Automaticaly register blackjack Win/Lost/Bust/Bank
 - Automaticaly register history of every Win/Lost/Bust/Banking with they amount
- 🔸 Real-time profit/loss results calculation
- 🔸 Fully in-game UI (/bank)
 - Dealer UI individualy from Player UI
 - Individual History for Dealer/Player modes
- 🔸 Editable message labels for Dealer mode
- 🔸 One-click copy to clipboard
- 🔸 Persistent configuration (saves automatically)
- **🔸See ingame for more...**

## **📦 Installation**
### _Custom Repository:_

1. Open Dalamud settings
2. Navigate to **Experimental → Custom Plugin Repositories**
3. Add `https://raw.githubusercontent.com/seventity7/GambaBank/main/repo.json`
4. Open plugin installer
5. Search for **GambaBank**
6. Install

## **🧠 How It Works**
### _Gamba Bank calculates your result using the formula:_

### Profiles
Create new profiles or edit existing ones. Each one saves its own configuration and history.

<img width="644" height="140" alt="Un43t122itled" src="https://github.com/user-attachments/assets/c3705c9a-f77f-419e-9723-5ae1798eced0" />

## Main Window
### Dealer Mode:
<img width="1077" height="522" alt="Unt12itled" src="https://github.com/user-attachments/assets/99686ed9-35c2-4ce4-9541-668c7cc607c0" />

- Result = `Starting Bank - Final Bank - Tips`
- Example: Starting Bank: `10.000.000`
- Final Bank: `5.000.000`
- Tips: `2.000.000`
- Result: `+ 3.000.000`

- Dealer history registers every Start/Final Bank, Tips, Date/Time, House/Club Name

<img width="1077" height="158" alt="Un3t122itled" src="https://github.com/user-attachments/assets/2b54f1e0-a949-450a-917d-8bbc97a3f358" />

- Create your own generated message to personalize your result message through the plugin config window.

<img width="1149" height="261" alt="Un43t1224itled" src="https://github.com/user-attachments/assets/56bdea94-e7bd-49f3-bd73-6624c20676f4" />


### Player Mode:
<img width="1077" height="522" alt="Unt122itled" src="https://github.com/user-attachments/assets/652e6664-99b5-4011-b27f-5cf43a181e25" />

- Realtime Bet/Banking Tracking
- Manual inputs foor registering Win/Loss _(if you want to)_
- Dealer tracking > it will pickup the match results only sent by that person


- Full history that shows every Round results with the Banking/Bet/Wins/Losses results

<img width="1027" height="197" alt="Un43t123234itled" src="https://github.com/user-attachments/assets/d2acb95e-bed5-4791-a844-2de8ee8b6688" />

- Realtime Bet/Banking Tracking
- Type your initial banking on `Starting Bank`, e.g: `10.000.000`
- Type the club/house if you want on `House`
- Type your current Bet
- Target the dealer and click "Track Dealer"
- Need to add more bank? Use the "Add Bank" section to keep the track going real-time

<img width="654" height="113" alt="Un43t12234itled" src="https://github.com/user-attachments/assets/6d176ebc-ab09-45e1-abbc-2d92a4d3a1e5" />

## **✏️ Customization**
### _Customize message labels directly in the UI:_

- Result Label → Today Profit/Loss:
- Starting Label → Starting Bank:
- Final Label → Final Bank:

## **👤 Author**

**Bryer**

## **💬 Support**
If you encounter issues or have suggestions:

- Open an issue on GitHub
- Or contact via Discord

## **📜 License**

This project is provided as-is for personal use within FFXIV.

<img width="1194" height="427" alt="sdd(1)" src="https://github.com/user-attachments/assets/4a00b3ce-a7f8-40ab-92b3-bdde453d3ea4" />
