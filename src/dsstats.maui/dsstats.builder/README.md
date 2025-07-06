
# dsstats builder

A tool to automatically build units in the Direct Strike Tutorial map (StarCraft II) based on replay data.

## Features
* Automatically places units according to replay information
* Uses simulated keyboard and mouse input
* Helps recreate builds for training or analysis

## Getting Started
1. **Close all other applications.**
2. Launch the Direct Strike Tutorial map in StarCraft II.
3. Assign hotkeys:
    * Set Team 1's worker (top player) to hotkey 1
    * Set Team 2's worker (bottom player) to hotkey 2
4. **Do not move the workers!**
    * For accurate unit placement, workers must remain at the center of their designated build zones.
5. Open dsstats, load the replay you want to test, open the desired build, and click "Build". (Screenshot TODO)
6. Switch back to StarCraft II and do not touch your mouse or keyboard until the build is complete.
7. Ensure no other application is in focus, as dsstats sends automated mouse and keyboard inputs that may interfere with active windows.

## How It Works
* Parses replay files to extract unit placement data
* Maps unit positions to screen coordinates
* Simulates precise keyboard and mouse inputs to replicate builds

## TODO
* top map build area corners
* check terran build
* check protoss build
* build hidden units (Lurker, Widowmine)
* ability/upgrades
* test other screen resolutions
* team 2 build / test
* Commanders builds