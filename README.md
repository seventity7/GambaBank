`Most recent update: 1.0.5`
# 

# **💰 Gamba Bank** `Readme Updated Apr 04 - 2026`

- Designed to help bothe `Players` and `Dealers` that loves to gamba and dont knwo how to track their gil gains and losses.
- It provides in-game tracker that formats values automatically and generates a ready-to-copy message for Dealers to share results.
- A full automatic real-time tracker for Players to know they current Bank and all Wins/Losses in a simple history

## **✨ Features**

- 🔸 Dealer Mode: Track Start Bank, Final Bank, Tips, Day results, All time profits/losses
 - Auto save Chat Logs, Trade Logs, Shift summary, Players that played, When players left, etc
- 🔸 Player mode: Real-time tracking of betting, losses, wins and banking
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
5. Search for **Gamba Bank**
6. Install

## **🧠 How It Works**
### _Gamba Bank have 2 modes, `Dealer` and `Player`_

### Profiles
Create new profiles or edit existing ones. Each one saves its own configuration and history.

<img width="649" height="37" alt="image" src="https://github.com/user-attachments/assets/02c88b6d-3b2d-4e72-b749-dbc4203de48e" />

#

### Main buttons
 - Settings: Open the settings window
 - Help: Open the Help window
 - Dealer: Enter Dealer mode
 - Player: Enter Player mode
 - ?: Quick help in tooltip format
 
<img width="382" height="35" alt="image" src="https://github.com/user-attachments/assets/bbccd627-0057-4749-b116-47e33d3f42ce" />

#

### Settings Window
 - Movable: Pin/Unpin the main window
 - Auto Clear After Copy: Clear the fields after hitting the Copy button
 - Backup options: `Daily` and `On Shift End`; Backup plugin logs/config
Edit message section
 - Use the options to customize the generated message from the main window

<img width="664" height="400" alt="image" src="https://github.com/user-attachments/assets/145d994b-d94f-48fb-81da-3f978d6bffbb" />

#

### Help Window
_See information with full detail about the plugin ingame_
> The information shown in this window can and will be changed from time to time

<img width="1048" height="824" alt="image" src="https://github.com/user-attachments/assets/49348b30-2820-4bb6-b8fd-0df5070db06e" />

#

## Dealer Mode:
### Current Dealer Session
Start your shift by clicking the `Start Shift` button.
 - You can do breaks by hitting the `Break` button
 - Hit `Resume` after your break finishes
After your shift is done, hit `End Shift`
You can see the shift Status, when it started and how much time has elapsed so far

<img width="378" height="80" alt="image" src="https://github.com/user-attachments/assets/9a820b11-3c53-4aed-a5bc-7f7735fd4d70" />

# 

Type your initial bank at the `Starting Bank` field
After your shift finishes, type the total remaining bank on the `Final Bank` field
Got any Tips? Type them on the `Tips` field
 - The plugin will automaticaly calculate your results
 - Use the field `House`to register the venue name _(not obligatory)_

<img width="1132" height="86" alt="image" src="https://github.com/user-attachments/assets/a38881c8-a65d-4bc9-87ce-2709c8e3a337" />

# 

### Generated Message

After your shift ends you will be able to `Copy` a message that already contains all information about your night results
 - You can send them in chat to your manager, if you have one

 <img width="740" height="59" alt="image" src="https://github.com/user-attachments/assets/12ffd7da-a4ab-4bd4-9e7a-0e7cf9b897ba" />

In the same Dealer session you will see the buttons `Turn logs ON` `Log History` and the tracker status
To be able to turn ON the logs, you need to be in a party
 - after enabling it - the plugin will automaticaly register the `Chat` `Trade` `Time joined` `When left` - logs from everyone in the party
 - You can check the logs clicking the button `Log History`

The tracker status will give you feedback, showing if the respective logs are being registered or not

<img width="696" height="118" alt="image" src="https://github.com/user-attachments/assets/9217aa8d-2c38-4608-b1da-5ed053b95063" />

#

### Dealer History
Here is shown everything related to your results and shift
Starting with a quick and compact way to see
 - `Today Results`
 - `Weekly Results`
 - `Monthly Results`
 - `Total Tips`

<img width="350" height="99" alt="image" src="https://github.com/user-attachments/assets/7f77ef26-d716-454b-a4d8-0b58248bd2e3" />

# 

In the same section you can view a full history containing
 - House name saved
 - Time `Date/Hour:Minute:Seconds`
 - Start Bank
 - Final Bank
 - Tips
Results shows the line respective values, also registering Shift checkpoints

<img width="1132" height="226" alt="image" src="https://github.com/user-attachments/assets/d6956213-15d1-476b-881b-7b5101a6324e" />

# 

You are able to export the entire history panel through
 - `.txt` Format
 - `.csv` Format _(Excel)_
Hit the folder icon to choose the save directory path and you are good to go

<img width="233" height="34" alt="image" src="https://github.com/user-attachments/assets/c4531d50-17c9-40d2-a349-05b756be9d3d" />

# 

### History Log
If you turned logs ON then everything will be logged here
 - Player name from party group
 - When Player joined
 - When Player left
 - Gil received from Player
 - Gil sent to Player
 - Chat Log; Download a .txt with every message the Player sent during the match
 - Trade Log; Download a .txt with every trade the Player sent/received during the match
Dealer is included. His Chat Log will include all Shift summary.
Dealer Trade Log will show every trade received and sent + they values

You can `Sort` the list by `Most Recent` `Today` `This Week` `This Month` and by `House` saved names

You can select lines from the history and use the `Bulk Actions` section to mass-save `Chat/Trade Logs` and/or edit house Saved names

> **.txt Logs files will be saved on the directory path choosed by the user. Is recommended to use a Folder for this. Deleted files will not be able to recover once deleted. The user can change the directory path at any time.**

<img width="1130" height="490" alt="image" src="https://github.com/user-attachments/assets/67c1d454-41b0-4a07-865f-a70822622d67" />

# 

## Player Mode:
### Bank Tracking
Type your starting bank amount in the field `Starting Bank` and save the venue name_(optional)_ in the field `House`
 - Your current bank will be registered automaticaly
 - Results will be shown automaticaly based on `Wins/Losses`
 - After hitting `Save`, all current values will be saved on `Player History`
You can save the current value as shown by clicking `Save` or delete all fields by clicking `Clear`

If you need to add more bank, do not change the `Starting Bank` field, use the `Add Bank` field instead.
 - Type the amount in the field where it says `Type bank here` and
 - Hit the button `Add Bank`

<img width="797" height="110" alt="image" src="https://github.com/user-attachments/assets/6a2f41a5-2414-4e11-a760-9eccb526b230" />

#

### Bet Tracking (AUTOMATIC)
Type your `Current Bet` in the respective field, it will be automaticaly placed in the `Blackjack` field too
Above the buttons `NAT BJ`/ `DIRTY BJ` there is they respective multipliers, setup them accordingly
Target the Dealer and hit the `Track Dealer` button; Then hit the button `Auto Track`
The plugin will automaticaly identify: _(You dont need to hit any other buttons)_
 - `Wins` when you win
 - `Loss` when you lose _(including Busts/DD/Splits)_
 - `NAT BJ`/`DIRTY BJ` when you get one
 - `Double Downs` when one happens
The fields `Current Bank` and `Results` will be also updated accordingly and everything will be automaticaly saved in the `Player History`
> **Real-time track still in <ins>Experimental</ins> phase, if it fails to detect anything, use the manual buttons to register it.**

<img width="609" height="122" alt="image" src="https://github.com/user-attachments/assets/c9fccc45-30b2-4a91-b23f-6c075f5ea339" />

The tracker status will show what is being tracked and what isnt. Nothing will be tracked if there is no Dealer tracked first

<img width="432" height="96" alt="image" src="https://github.com/user-attachments/assets/191bfa5e-7176-4578-bca7-07f251b2a89f" />

#

### Bet Tracking (MANUALY)
Type your `Current Bet` in the respective field, it will be automaticaly placed in the `Blackjack` field too
Above the buttons `NAT BJ`/ `DIRTY BJ` there is they respective multipliers, setup them accordingly
You can track everything manualy by registering everything through the buttons
 - Hit `Win` when you win
 - Hit `Loss` when you lose _(including Busts/DD/Splits)_
 - Hit `NAT BJ`/`DIRTY BJ` when you get one
The fields `Current Bank` and `Results` will be updated accordingly and everything will be automaticaly saved in the `Player History`

<img width="437" height="130" alt="image" src="https://github.com/user-attachments/assets/ff93e09a-5751-4571-9280-e8c0cf94dc4c" />

#

### Player History
Here is shown everything related to your match results
You can view a full history containing
 - House name saved
 - Time `Date/Hour:Minute:Seconds` of that result
 - Start Bank
 - Final Bank
 - Results such as `Win`; `Loss`; `Push`; `Bust`; `DoubleDown`; `BlackJack`

<img width="787" height="270" alt="image" src="https://github.com/user-attachments/assets/6a1f0521-700c-48d7-bbfd-7a10c54209f1" />

# 

You are able to export the entire history panel through
 - `.txt` Format
 - `.csv` Format _(Excel)_
Hit the folder icon to choose the save directory path and you are good to go

<img width="233" height="34" alt="image" src="https://github.com/user-attachments/assets/c4531d50-17c9-40d2-a349-05b756be9d3d" />

#

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
