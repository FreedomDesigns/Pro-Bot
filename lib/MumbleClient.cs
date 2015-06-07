using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Protocol.Mumble;
using Mono.Data.Sqlite;
using System.Globalization;
using System.Diagnostics;
using System.Threading;

namespace Protocol.Mumble
{
    public class MumbleClient : MumbleConnection
    {
        #region Vars

        private readonly Dictionary<UInt32, MumbleChannel> channels = new Dictionary<UInt32, MumbleChannel>();

        public Dictionary<UInt32, MumbleChannel> Channels
        {
            get
            {
                return channels;
            }
        }

        public MumbleChannel RootChannel { get; internal set; }

        private Dictionary<UInt32, MumbleUser> users = new Dictionary<UInt32, MumbleUser>();

        public Dictionary<UInt32, MumbleUser> Users
        {
            get
            {
                return users;
            }
        }

        public MumbleUser ClientUser { get; private set; }

        public string Version { get; private set; }

        public uint serverVersion;
        public string ServerOS { get; private set; }
        public string ServerOSVersion { get; private set; }
        public string ServerRelease { get; set; }

        public string WelcomeText { get; private set; }
        public uint MaxBandwith { get; private set; }

        public event EventHandler<MumblePacketEventArgs> OnConnected;
        public event EventHandler<MumblePacketEventArgs> OnTextMessage;

        public string DB;
        public string BotVersion;
        private DateTime startTime = DateTime.Now;

        private Thread ChannelThread;

        #endregion

        #region Connect, Update and Database

        public MumbleClient(string version, string host, string username, int port = 64738) :
            base(host, username, port)
        {
            Version = version;
            OnPacketReceived += ProtocolHandler;
        }

        public new void Connect()
        {
            System.Threading.Thread.Sleep(100); //This allows time for the DB name to transfer across.
            base.Connect();

            SendVersion(Version);
            SendAuthenticate();
            SendFreedom();
            SendVersion("1.0");

            ChannelThread = new Thread(CodingMe);
            ChannelThread.Start();         
        }

        private void CodingMe()
        {
            var channel = FindChannel("Coding");
            SwitchChannel(channel);
        }

        private void ProtocolHandler(object sender, MumblePacketEventArgs args)
        {
            var proto = args.Message as IProtocolHandler;

            proto.HandleMessage(this);
        }

        public void Update(Version message)
        {
            ServerOS = message.os;
            ServerOSVersion = message.os;
            ServerRelease = message.release;
            serverVersion = message.version;
        }

        public void Update(ServerSync message)
        {
            if (message.sessionSpecified) { ClientUser = users[message.session]; }
            if (message.max_bandwidthSpecified) { MaxBandwith = message.max_bandwidth; }
            if (message.welcome_textSpecified) { WelcomeText = message.welcome_text; }

            DispatchEvent(this, OnConnected, new MumblePacketEventArgs(message));
        }

        /*public void GiveMe()
        {
            DB = database;
            BotVersion = verzon;
        }*/

        public void DoIKnowYou(string database, string verzon)
        {
            DB = database;
            BotVersion = verzon;
        }

        #endregion

        #region Text and Commands

        public void Update(TextMessage message)
        {
            DispatchEvent(this, OnTextMessage, new MumblePacketEventArgs(message));

            // Sends the message and actor to the command thread
            string messages = message.message;
            ChannelMessageReceived(message.actor, message.message);

        }

        //*************   COMMANDS BY FREEDOM   *************/

        // TODO: Change all var and string names to better one.

        protected virtual void ChannelMessageReceived(uint actor, string message)
        {
            // Gets the userID for sending messages
            var User = FindUser(actor);

            // Connects the database
            SqliteConnection m_dbConnection;
            m_dbConnection = new SqliteConnection("Data Source=" + DB + ";Version=3;");
            m_dbConnection.Open();

            // Checks if message was a command
            if (message.StartsWith("!"))
            {
                // Absys request as normal :P
                string[] Str = message.Split(new char[] { ' ' }, 2);
                string Command = Str[0].ToLower();

                /* ## All Commands are cased below ## */
                switch (Command)
                {
                    case "!help":
                        /*--------------------------------------------------------------------*
                         * Shows the user what commands they can use and also how to use them *
                         *--------------------------------------------------------------------*/

                        if (Str.Length == 1)
                        {
                            SendTextMessageToUser("<b>!ver,!seen,!time,!move,!msg,!about,!sayall,!uptime</b>", User);
                        }
                        else
                        {
                            switch (Str[1])
                            {
                                case "ver":
                                    SendTextMessageToUser("<b>!ver - Give the current version of Bot.</b>", User);
                                    break;
                                case "seen":
                                    SendTextMessageToUser("<b>!seen [User Name] - When was this player last seen? </b>", User);
                                    break;
                                case "time":
                                    SendTextMessageToUser("<b>!time - display the servers current time.</b>", User);
                                    break;
                                case "move":
                                    SendTextMessageToUser("<b>!move [Channel Name] - Moves the bot to the channel chosen.</b>", User);
                                    break;
                                case "msg":
                                    SendTextMessageToUser("<b>!msg [User Name] [Message] - Will send the message to the user when they are next online.</b>", User);
                                    break;
                                case "about":
                                    SendTextMessageToUser("<b>!about - Shows the bot creator and info about it.</b>", User);
                                    break;
                                case "uptime":
                                    SendTextMessageToUser("<b>!uptime - Displays the current uptime of Pro-Bot.</b>", User);
                                    break;
                                case "sayall":
                                    SendTextMessageToUser("<b>!sayall [Message] - Sends message to every channel in server.</b>", User);
                                    break;
                                default:
                                    SendTextMessageToUser("<b><span style=\"color:#ff0000\">[ERROR]</span> Command does not exist - try use !help.</b>", User);
                                    break;
                            }
                        }
                        break;
                    case "!ver":
                        /*---------------------------------------------*
                         * Will display the current version of the bot *
                         *---------------------------------------------*/

                        SendTextMessageToUser(string.Format("<b>Currently Running Version: {0}</b>", BotVersion), User);
                        break;
                    case "!time":
                        /*--------------------------------------------------*
                         * Will display the current time of the bot or user *
                         *--------------------------------------------------*/

                        SendTextMessageToUser("<b>The current time is: " + DateTime.Now.ToString("HH:mm:ss tt") + "</b>", User);
                        break;
                    case "!seen":
                        /*-----------------------------------------------------------*
                         * Will tell if the user id online or when they were last on *
                         *-----------------------------------------------------------*/

                        if (Str.Length == 1)
                        {
                            SendTextMessageToUser("<b><span style=\"color:#ff0000\">[ERROR]</span> You must supply a user name</b>", User);
                            break;
                        }

                        string timeSeen = "";
                        string userName = Str[1].ToUpper();

                        string getAll = "SELECT * FROM `Users` WHERE `Name`='" + userName + "'";
                        SqliteCommand CMD = new SqliteCommand(getAll, m_dbConnection);
                        CMD.ExecuteNonQuery();

                        // Reads getAll result
                        using (SqliteDataReader rdr = CMD.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                string onlineNow = "" + rdr["Online"];
                                if (onlineNow != "1")
                                {
                                    timeSeen = "" + rdr["LastSeen"];
                                    SendTextMessageToUser("<b>" + userName + " was last on: " + timeSeen + "</b>", User);
                                }
                                else
                                {
                                    SendTextMessageToUser("<b>" + userName + " is online now</b>", User);
                                }
                            }
                        }
                        break;
                    case "!move":
                        /*-----------------------------------------*
                         * Will move the bot to the channel listed *
                         *-----------------------------------------*/

                        if (Str.Length == 1)
                        {
                            SendTextMessageToUser("<b><span style=\"color:#ff0000\">[ERROR]</span> You must supply a channel name</b>", User);
                            break;
                        }

                        // Another amazing idea from never guess who? [Absy]  

                        // Made string to all lowercase and then to upper all first char on each word [Idea = Absy writes correctly]
                        // NOTE: May be an issue for Away From Keyboard but why would a bot be AFK. - Freedom
                        string Ilikesmall = Str[1].ToLower();
                        string Ilikesomesmall = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Ilikesmall);

                        var channelName = Ilikesomesmall.TrimEnd(' ');

                        if (channelName == null) { return; }

                        var channel = FindChannel(channelName);

                        if (channel == null) 
                        {
                            SendTextMessageToUser("<b><span style=\"color:#ff0000\">[ERROR]</span> Could not find channel (Try use exact characters).</b>", User); 
                            return;
                        }

                        SwitchChannel(channel);
                        break;
                    case "!msg":
                        /*------------------------------------------------------------------*
                         * Will send a message to the user for when they next appear online *
                         *------------------------------------------------------------------*/

                        if (Str.Length == 1)
                        {
                            SendTextMessageToUser("<b><b><span style=\"color:#ff0000\">[ERROR]</span> You must supply a user name and a message</b>", User);
                            break;
                        }

                        string[] MessageMe = Str[1].Split(new char[] { ' ' }, 2);

                        //Console.WriteLine(MessageMe[0]);
                        //Console.WriteLine(MessageMe[1]);

                        string NameMe = "";

                        string GetName = "SELECT * FROM `Users` WHERE `Session`='" + actor + "'";
                        SqliteCommand NameIt = new SqliteCommand(GetName, m_dbConnection);
                        NameIt.ExecuteNonQuery();


                        using (SqliteDataReader rdr = NameIt.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                NameMe = "" + rdr["Name"];

                            }
                        }
                        string OhHey = "INSERT INTO Messages (`To`, `From`, `Message`, `Recived`) VALUES ('" + MessageMe[0].ToUpper() + "', '" + NameMe + "', '" + MessageMe[1] + "' , '0')";
                        SqliteCommand command = new SqliteCommand(OhHey, m_dbConnection);
                        command.ExecuteNonQuery();
                        break;
                    case "!about":
                        SendTextMessageToUser("<b>Pro-Bot was designed and coded by Freedom, with the main protocol based off Mumble.Net</b>", User);
                        break;
                    case "!uptime":
                        var delta = DateTime.Now - startTime;
                        TimeSpan _diff = DateTime.Now.Subtract(Process.GetCurrentProcess().StartTime);
                        string runtime = string.Format("{0} days and {1:00}:{2:00}:{3:00}", (int)_diff.TotalDays, _diff.Hours, _diff.Minutes, _diff.Seconds);
                        SendTextMessageToUser("<b>Pro-Bot current uptime is: " + runtime + "</b>", User);
                        break;
                    case "!sayall":
                        if (Str.Length == 1)
                        {
                            SendTextMessageToUser("<b><span style=\"color:#ff0000\">[ERROR]</span> You must supply a message.</b>", User);
                            break;
                        }
                        var AmzChannel = FindChannel("Root");
                        SendTextMessageToChannel("<b>" + Str[1] + "</b>", AmzChannel, true);
                        break;
                    default:
                        /*------------------------------------------------------------------------*
                         * Will tell the user if the command does not exist or if they just put ! *
                         *------------------------------------------------------------------------*/

                        SendTextMessageToUser("<b><span style=\"color:#ff0000\">[ERROR]</span> '!' Must provide a command [try !Help].</b>", User);
                        break;
                }
            }
        }

        public void SendTextMessageToUser(string message, MumbleUser user)
        {
            SendTextMessage(message, null, null, Enumerable.Repeat(user, 1));
        }

        public void SendTextMessageToChannel(string message, MumbleChannel channel, bool recursive)
        {
            if (recursive)
            {
                SendTextMessage(message, null, Enumerable.Repeat(channel, 1), null);
            }
            else
            {
                SendTextMessage(message, Enumerable.Repeat(channel, 1), null, null);
            }
        }

        #endregion

        #region Channel Switch

        public void SwitchChannel(MumbleChannel channel)
        {
            SendUserState(channel);

        }

        #endregion

        #region Find Area

        public MumbleChannel FindChannel(string name)
        {
            return channels.Values.Where(channel => channel.Name == name).FirstOrDefault();
        }

        public MumbleUser FindUser(uint id)
        {
            return users.ContainsKey(id) ? users[id] : null;
        }

        private UInt64 sequence = 1;

        internal UInt64 NextSequence()
        {
            return sequence += 2;
        }

        #endregion
    }
}
