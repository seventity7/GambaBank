`Changelog 1.0.2 - 03/29/2026`

## ✨ Added:

- Real-time Calculation
- Results now update instantly while typing
- Removed need for confirm button

➕ Tips System
- Added Tips field fully integrated into calculation

**Math Formula:**
`{FinalBank} - {StartingBank} - {Tips} = {Results}`

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
Undo last saved entry instantly

📋 Save to History
Manual session saving button added

❓ Help / Formula Tooltip
New help button (?)
Hover → temporary tooltip
Click → persistent tooltip
Click again → close

📎 Copy Feedback
_Copy button now shows:_
`✓ Copied`

🎛️ New Command
`/banksettinngs` → opens config window

## 🛠️ Fixed

🧮 Fixed incorrect result calculations
📉 Fixed profit/loss logic (tips now included correctly)
🧷 Fixed UI misalignment (labels + fields)
🪟 Fixed window clipping issues
🎨 UI Overhaul
🎯 Layout Improvements
🎨 Button Styling
🟢 Primary buttons (Copy, Save to history)
⚙️ Utility buttons (Undo, Clear History)
🔴 Danger button (Delete Current)
✨ Hover Effects
❓ Help Button
📊 Table Colors
🟡 Date / Bank → gold
⚪ Tips → white
🟢 Results → positive
🔴 Results → negative

## 📝 Improved

⚡ Faster input handling
🧠 Cleaner calculation logic
📦 More consistent history formatting
🔄 Improved profile handling
🧾 Better generated message formatting
🎛️ UI / UX
❓ Interactive help system
🧼 Cleaner header layout
🧷 Better spacing between sections
📐 More consistent alignment

## 🧾 Notes

Formula formatting:
✔ Positive results → +
❌ Negative results → -
📂 History is saved per profile
📎 Copy can include timestamp (optional)

## 📌 Summary
_This update focuses on:_

⚡ Speed (real-time updates)
🎨 UI polish
📊 History & data control
🧠 Better usability and QOL

`End of changelog`

# 💰 Gamba Bank

Designed to help players track their gil gains and losses in a simple and clean way.

It provides in-game calculator that formats values automatically and generates a ready-to-copy message for sharing results.

**## ✨ Features**

🔸 Track Start Bank, Final Bank, Tips & Results
🔸 Automatic number formatting
🔸 Real-time profit/loss calculation
🔸 Positive/negative result detection (+ / -)
🔸 Fully in-game UI (/bank)
🔸 Editable message labels
🔸 One-click copy to clipboard
🔸 Persistent configuration (saves automatically)
**🔸See github for more...**

**## 📦 Installation**
### Custom Repository:

1. Open Dalamud settings
2. Navigate to **Experimental → Custom Plugin Repositories**
3. Add `https://raw.githubusercontent.com/seventity7/GambaBank/main/repo.json`
4. Open plugin installer
5. Search for **GambaBank**
6. Install

**## 🧠 How It Works**
### Gamba Bank calculates your result using the formula:

Result = `Starting Bank - Final Bank - Tips`
Example:
Starting Bank: `10.000.000`
Final Bank: `5.000.000`
Tips: `2.000.000`
Result: `+ 3.000.000`

**## 💬 Generated Message**
###The plugin automatically generates a formatted message:

Today Profit/Loss: + 3.000.000 Gil | Starting Bank: 10.000.000 Gil | Final Bank: 5.000.000 Gil
You can copy this instantly using the Copy button.

**## ✏️ Customization**
###Customize message labels directly in the UI:

Result Label → Today Profit/Loss:
Starting Label → Starting Bank:
Final Label → Final Bank:

## 👤 Author

**Bryer**

---

## 💬 Support

If you encounter issues or have suggestions:

- Open an issue on GitHub
- Or contact via Dalamud community

## 📜 License

This project is provided as-is for personal use within FFXIV.