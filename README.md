Pro-Bot | Mumble Administration Bot
=================================================================================

Created by Freedom (https://www.youtube.com/user/FreedomDesignsz)  
Co-Created by Absy   

Brief Summary
=================================================================================



This project aims to provide the ability to have a automated way of 
controlling and logging your server with ease and quickly.

Installation/ Utilization
=================================================================================
Linux
-----
* Install Mono-Compleate
```
apt-get install mono-complete
```
// Adding Soon


Windows
-----
```
Adding Soon
```



Feature List
=================================================================================

- Chat logging of channel the bot is connected to.
- Custom commands.
- More Coming Soon.

Levels
=================================================================================

> `Format - [Group Name - Group Level]`

* `Guest - 0`
   * User who has joined for first time or user who has not registered yet
   * Has restricted use of commands.
* `Registered - 25`
   * User who has registered
   * Has more commands that are basic.
* `Mod - 50`
   * Trusted user
   * Has more commands that help moderate the server
* `Admin - 75`
   * Very trusted user
   * Has nearly all commands expect the important commands.
* `SuperAdmin - 100`
   * Main administrators of server
   * Has full control over bot.

Command List
=================================================================================
* `!ver`
   * Level 0   
   * Displays the current version of pro-bot
* `!uptime`
   * Level 0  
   * Displays the current runtime of pro-bot
* `!time`
   * Level 0  
   * Displays the current time of pro-bot
* `!about`
   * Level 0  
   * Displays information about pro-bot
* `!admins`
   * Level 0  
   * Displays the current users with levels 50 or above that are online
* `!register`
   * Level 0  
   * Sets the users group to registered [25]
* `!lastseen <user name>`
   * Level 25  
   * Displays the current status of the user with either 'User is online now' or 'User was last online'
* `!msg <user name> <message>`
   * Level 25  
   * Stores a message to be displayed to the user specified when they next appear online
* `!regtest `
   * Level 25  
   * Displays the current users group name and level
* `!warn <user name>`
   * Level 50  
   * Removes user specified and resets them back to guest [0]
* `!sayall <message>`
   * Level 50  
   * Sends the message specified to all channels
* `!move <channel name>`
   * Level 75  
   * Moves pro-bot the channel specified
* `!putgroup <group name> <user name>`
   * Level 100  
   * Adds user specified to the group specified
* `!ungroup <user name>`
   * Level 100  
   * Removes user specified and resets them back to guest [0]

