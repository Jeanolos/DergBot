# DergBot
Twitch Bot focused on integration with HTML overlays written in C#

Free, open, solid and feature rich Twitch Bot for everyone to use and to improve.

# Social Media
To stay tuned about latest updates, we recommend you to follow our Twitter accounts:
- Jeanolos | https://twitter.com/tgr_jeanolos
- Ocgineer | https://twitter.com/ocgineer

# Features / TODO
The basic stuff like any other Twitch bot out there but with additions;
- Chatty like chat, where you can open individual sub-chats of Viewers for easier access to their messages. ✔
- Output data (as much as viable/usefull) to json/text files (possibly websocket) for use in dynamic html5 overlays.
- Multiple giveaway system with each their own settings and entry list for when one is not enough. ✔
- Discord integration -> set the playing game to for example 'LIVE: {game set on Twitch}'. **Not needed anymore, stupid Discord...**
- Gamewisp integration for non-partners or when the subbutton is not good enough.
- Ranks (best of two worlds, Deepbot and Ankhbot);
  - Viewer
  - Regular
  - VIP Bronze
  - VIP Silver
  - VIP Gold
- Import Points and Hours from Deepbot and Ankhbot and cleanup database; ✔ (partially)
  - Remove Viewers below x amount of points
  - Remove Viewers below x amount of hours
  - Remove Viewers longer than x days last seen
- What we can change on Twitch through API will most likely to get a spot, just to make things simple.
- Own alerts (in the end) where donations are handled through TwitchAlerts as we don't like swapping

# How to compile and use
At the moment, the bot is under heavy development!
That means, it is full of bugs and it lacks of many features. We will release the bot, once the majority of bugs is fixed, but if you still want to use the bot in this state, here are instructions:

  1. Install Visual Studio 2015 on your computer
  2. Install the GitHub Extension for Visual Studio
  3. Add an existing repository and enter the link of this bot
  4. Sync it!
  5. Press Build and look into the projects folder (C:\Users\{yourname}\Source\Repos\OakBot\OakBot\bin\Debug
  6. Run the Oakbot.exe file

If you've never used Visual Studio before or haven't even coded before, there will be a bleeding branch for development releases.
