**`Changelog 1.0.2 - 03/29/2026`**

## **✨ Added:**

- Real-time Calculation
- Results now update instantly while typing
- Removed need for confirm button


➕ Tips System
- Added Tips field fully integrated into calculation


📊 History System
_Stores full session data:_
- Date
- Time
- Starting Bank
- Final Bank
- Tips
- Results


🔽 Sorting System
_Sort history by:_
- Most recent
- Oldest (ascending)
- Highest results
- Highest tips


↩️ Undo System
_Undo last saved entry instantly_


📋 Save to History
_Manual session saving button added_


❓ Help / Formula Tooltip
 - New help button (?)
 - Hover → temporary tooltip
 - Click → persistent tooltip
 - Click again → close


📎 Copy Feedback
 - _Copy button now shows:_
 - `✓ Copied`


🎛️ New Command
 - `/banksettinngs` → opens config window


## **🛠️ Fixed**

- 🧮 Fixed incorrect result calculations
- 📉 Fixed profit/loss logic (tips now included correctly)
- 🧷 Fixed UI misalignment (labels + fields)
- 🪟 Fixed window clipping issues
- 🎨 UI Overhaul
- 🎯 Layout Improvements
- 🎨 Button Styling
- 🟢 Primary buttons (Copy, Save to history)
- ⚙️ Utility buttons (Undo, Clear History)
- 🔴 Danger button (Delete Current)
- ✨ Hover Effects
- ❓ Help Button
- 📊 Table Colors
- 🟡 Date / Bank → gold
- ⚪ Tips → white
- 🟢 Results → positive
- 🔴 Results → negative


## **📝 Improved**

- ⚡ Faster input handling
- 🧠 Cleaner calculation logic
- 📦 More consistent history formatting
- 🔄 Improved profile handling
- 🧾 Better generated message formatting
- 🎛️ UI / UX
- ❓ Interactive help system
- 🧼 Cleaner header layout
- 🧷 Better spacing between sections
- 📐 More consistent alignment


## **🧾 Notes**
### _Formula formatting:_

- ✔ Positive results → +
- ❌ Negative results → -
- 📂 History is saved per profile
- 📎 Copy can include timestamp (optional)


## **📌 Summary**
### _This update focuses on:_

- ⚡ Speed (real-time updates)
- 🎨 UI polish
- 📊 History & data control
- 🧠 Better usability and QOL


**`End of changelog`**


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
