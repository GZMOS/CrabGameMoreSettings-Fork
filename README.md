# CrabGameMoreSettings
A BepInEx mod for Crab Game that adds more settings to the game.

## Additional Settings
- Alternate (extra) keybind options for jump and crouch.
- Hold to jump, allows you to hold the jump button to continuously jump (the same as holding jump in Minecraft).
- Hold to sprint (enabled by default), when disabled, the sprint key will become a toggle, the same way disabling hold to crouch makes it a toggle.
- Hold to interact, makes holding the interact button interact with whatever you're looking it continuously without needing to spam it (the same as holding the interact key on interactables in Minecraft, yes I used a Minecraft comparison a second time).
- and Hold to attack, makes holding the attack button continuously punch when holding nothing, swing when holding a melee weapon, fire when holding a gun, and throw when holding a snowball.

It should be noted that hold to jump, interact, and sprint all respect the vanilla game's cooldowns, meaning you cannot...
-  Press the ready button more than once per second (1 second cooldown)
-  Punch more than twice per second (0.5 second cooldown)
-  Use items during their swing/fire/throw cooldowns
-  Ect.

## Voice Chat Indicator
Though this isn't a setting per se, in the TAB menu you will now see microphone icons next to anyone that is currently talking (aka has sound coming through their microphone in audible range of you).
![Screenshot (910)](https://github.com/user-attachments/assets/21041e66-c273-4c0f-bbaf-0976dcf918a0)

This can be helpful to find and identify mic spammers in larger/packed lobbies faster and mute or kick them!

Note that internally this doesn't directly listen/check for if the player actually has audio coming through their mic, it actually just checks if the mouth of the player's character is moving (which does only move when it detects audio).
However this check only occurs every 0.5 seconds, meaning sudden/short sounds can occur between this range and not be detected.

## Steam Profile Viewer
When clicking the three dots next to a player's name in the Tab menu, you can now select "Profile" to open the Steam Overlay and view the player's profile.
