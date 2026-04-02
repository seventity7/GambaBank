## `Changelog 1.0.3 - March 31-2026`
`+Minor follow up 1.0.3.1/1.0.3.2/1.0.3.3 Apr 01 > Apr 02-2026`

## **✨ Added:**

📊 Tracker Status Panel
- Added a live **Tracker Status** panel in Player mode
- Shows tracking state for:
  - Wins
  - Losses
  - Blackjacks
  - Double-Downs


📒 Dealer Session Tools
- Added session checkpoints:
  - Start Shift
  - Break
  - Resume
  - End Shift
- Added current session status display for Dealer mode
- Added quick tips buttons for faster tip entry


📁 History System Improvements
- Added grouped history display by day
- History groups can now be expanded or collapsed
- Added clickable history entries for quick detail viewing
- Added a Dealer session summary viewer


📤 Export & Backup
- Added history export in:
  - TXT
  - Spreadsheet-style CSV/Excel-compatible format
- Added export directory selection
- Added automatic Dealer backups:
  - on End Shift
  - daily backup support
- Backup entries are now registered clearly in history


🧠 Smart Input Helpers
- Added House field suggestions/autocomplete
- Added better export path feedback after saving files
- Added inline export directory picker from the history panel


❓ Help & Settings Access
- Added quick access to **Settings** from the main top bar


## **🛠️ Fixed**

- Fixed tracking flow issues where some live results were not being applied correctly
- Fixed Player History saving so tracked changes are recorded more accurately
- Fixed repeated-result handling during Auto Track sessions


## **📝 Improved**

- 🎨 Improved visual organization across both Dealer and Player layouts
- 🧭 Cleaner separation between tracking, banking, and history tools
- 📈 Better readability for history results with color-based feedback
- 📂 Export flow is now easier to access directly from the history area
- 🖱️ More interactive history behavior with grouped dates and clickable entries
- ⚡ Faster Dealer workflow for session logging and tip entry
- 🎯 Player mode now feels more complete for active live-session use


## **🧾 Notes**
### _Current behavior:_

- Dealer and Player mode keep their own dedicated history flow
- Auto backups use the configured backup directory when available
- History exports can be redirected to a custom export folder
- Player tracking tools are designed for live use during active matches


## **📌 Summary**
### _This update focuses on:_

- 🎮 Stronger Player mode tracking
- 📒 Better Dealer session control
- 📤 Easier export and backup handling
- 🧠 Smarter history organization
- 🎨 UI / UX polish
- 🛠️ Stability and workflow improvements


**`End of changelog 1.0.3.3`**
# 

# **💰 Gamba Bank** `Readme Updated Apr 01 - 2026`

- Designed to help players track their gil gains and losses in a simple and clean way.
- It provides in-game calculator that formats values automatically and generates a ready-to-copy message for sharing results.

## **✨ Features**

- 🔸 Dealer Mode: Track Start Bank, Final Bank, Tips, Day results, All time profits/losses
- 🔸 Player mode: Real-time tracking of betting, losses, wins and banking
 - Automaticaly register blackjack Win/Lost/Bust/Bank
 - Automaticaly register history of every Blackjack/Win/Lost/Bust/Banking with they amount
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
<img width="1076" height="520" alt="Untit3led" src="https://github.com/user-attachments/assets/53649096-299d-42b9-9176-ebd7ca61eab0" />


- Result = `Starting Bank - Final Bank - Tips`
- Example: Starting Bank: `10.000.000`
- Final Bank: `5.000.000`
- Tips: `2.000.000`
- Result: `+ 3.000.000`

Dealer history registers every Start/Final Bank, Tips, Date/Time, House/Club Name

<img width="1077" height="158" alt="Un3t122itled" src="https://github.com/user-attachments/assets/2b54f1e0-a949-450a-917d-8bbc97a3f358" />

Create your own generated message to personalize your result message through the plugin config window.

<img width="1149" height="261" alt="Un43t1224itled" src="https://github.com/user-attachments/assets/56bdea94-e7bd-49f3-bd73-6624c20676f4" />


### Player Mode:

- Type your initial banking on `Starting Bank`, e.g: `10.000.000`
- Type the club/house if you want on `House`
- Type your current Bet
- Target the dealer and click `Track Dealer`
- Click `Auto Track`

<img width="1076" height="516" alt="Un2tit3led" src="https://github.com/user-attachments/assets/1558023a-23ac-4ced-9f95-4a5bf57b8290" />


- Realtime Bet Tracking
- Manual inputs foor registering Win/Loss _(if you want to)_
- Dealer tracking > it will pickup the match results only sent by that person

Blackjack(21) prize needs to be registered manually through the buttons/multipliers for NatBJ and DirtyBJ
- Use the multiplier buttons to select it accordling for Nat/Dirty. Default: `Nat: 2.5x` & `Dirty: 1.0x`
- Gamba bank will automaticaly register the prize into your `Banking` and `Player history` once pressed
<img width="160" height="90" alt="Un234tit53led" src="https://github.com/user-attachments/assets/db4bbc5b-4ffc-40a6-8902-abd02a2e1b0a" />


Full history that shows every Round results with the Blackjack/Banking/Bet/Wins/Losses results

<img width="1076" height="236" alt="Un2tit53led" src="https://github.com/user-attachments/assets/ad415301-986a-4186-8ded-a84fa5c51ee2" />


Need to add more bank? Use the "Add Bank" section to keep the track going real-time
- Real time bank tracking

<img width="1076" height="102" alt="Un23tit53led" src="https://github.com/user-attachments/assets/a9657e25-a920-445a-a866-0714be32478e" />


## **✏️ Customization**
### _Customize message labels directly in the UI:_

- Result Label → Today Profit/Loss:
- Starting Label → Starting Bank:
- Final Label → Final Bank:

## **👤 Author**

**Bryer**

## **💬 Support/Help**
_If you encounter issues or have suggestions:_

- Open an issue on GitHub
- Or contact via Discord

You can also type `/gamba help`or `/bank help` in-game to see the plugin Help window
<img width="1036" height="749" alt="Un1titled" src="https://github.com/user-attachments/assets/6e50835e-6a58-44ea-8bf2-b3cd7f466c16" />


## **📜 License**

This project is provided as-is for personal use within FFXIV © Do not distribute/modify without permission.

<img width="1194" height="427" alt="sdd(1)" src="https://github.com/user-attachments/assets/4a00b3ce-a7f8-40ab-92b3-bdde453d3ea4" />
