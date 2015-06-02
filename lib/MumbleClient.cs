using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Protocol.Mumble;
using Mono.Data.Sqlite;

namespace Protocol.Mumble
{
    public class MumbleClient : MumbleConnection
    {
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

        public string DB = @"XNP.Sqlite";



        public MumbleClient(string version, string host, string username, int port = 64738) :
            base(host, username, port)
        {
            Version = version;
            OnPacketReceived += ProtocolHandler;
        }

        public new void Connect()
        {
            base.Connect();

            SendVersion(Version);
            SendAuthenticate();
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

        public void Update(TextMessage message)
        {
            DispatchEvent(this, OnTextMessage, new MumblePacketEventArgs(message));
            //Console.WriteLine("Name: " + message.actor + " Message: " + message.message + " Channel ID: " + message.channel_id);
            //ChannelMessageReceived(new ChannelMessage(message.actor , string.Join("", message.message), message.channel_id, true));

            string messages = message.message;

                ChannelMessageReceived(message.actor, message.message);

        }

        protected virtual void ChannelMessageReceived(uint actor, string message)
        {
            // # Does user info
            var User = FindUser(actor);

            //Console.WriteLine(ChannelID);

            SqliteConnection m_dbConnection;
            m_dbConnection = new SqliteConnection("Data Source=" + DB + ";Version=3;");
            m_dbConnection.Open();

            if (message.StartsWith("!"))
            {
                // Absys request
                string[] Str = message.Split(new char[] { ' ' }, 2);
                string Command = Str[0].ToLower();

                if (Str.Length == 0)
                {
                    Str[1] = "";
                }

                if (Command.StartsWith("!help"))
                {
                    string info = "";

                    if (Str.Length == 1)
                    {
                        SendTextMessageToUser("<b>!ver,!seen,!time,!move</b>", User);
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
                            default:
                                SendTextMessageToUser("<b>[ERROR] Command does not exist - try use !help.</b>", User);
                                break;
                        }
                    }
                }
                else if (Command == "!ver")
                {
                    SendTextMessageToUser("<b>Currently Running Version 0.1</b>", User);
                }
                else if (Command == "!time")
                {
                    SendTextMessageToUser("<b>The current time is: " + DateTime.Now.ToString("HH:mm:ss tt") + "</b>", User);
                }
                else if (Command.StartsWith("!seen"))
                {
                    string Timeme = "";
                    string UserName = Str[1].ToUpper();

                    string Exists = "SELECT * FROM `Users` WHERE `Name`='" + UserName + "'";
                    SqliteCommand CMD = new SqliteCommand(Exists, m_dbConnection);
                    CMD.ExecuteNonQuery();

                    using (SqliteDataReader rdr = CMD.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            string onlineNow = "" + rdr["Online"];
                            if (onlineNow != "1")
                            {
                                Timeme = "" + rdr["LastSeen"];
                                SendTextMessageToUser("<b>" + UserName + " was last on: " + Timeme + "</b>", User);
                            }
                            else
                            {
                                SendTextMessageToUser("<b>" + UserName + " is online now", User);
                            }
                        }
                    }
                }
                /*else if (Command == "!say")
                {
                    var channel = FindChannelID(ChannelID);

                    if (channel == null) { return; }

                    SendTextMessageToChannel("<b>The current time is: " + DateTime.Now.ToString("HH:mm:ss tt") + "</b>", channel, false);
                }*/
                else if (Command.StartsWith("!move"))
                {
                    var channelName = Str[1];

                    if (channelName == null) { return; }

                    var channel = FindChannel(channelName);

                    if (channel == null) { return; }

                    SwitchChannel(channel);
                }
                else if (Command.StartsWith("!msg"))
                {
                    string[] MessageMe = Str[1].Split(new char[] { ' ' }, 2);

                    //Console.WriteLine(MessageMe[0]);
                    //Console.WriteLine(MessageMe[1]);

                    string Timeme = "";

                    string Exists = "SELECT * FROM `Users` WHERE `Session`='" + actor + "'";
                    SqliteCommand CMD = new SqliteCommand(Exists, m_dbConnection);
                    CMD.ExecuteNonQuery();


                    using (SqliteDataReader rdr = CMD.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            Timeme = "" + rdr["Name"];

                        }
                    }
                    string OhHey = "INSERT INTO Messages (`To`, `From`, `Message`, `Recived`) VALUES ('" + MessageMe[0].ToUpper() + "', '" + Timeme + "', '" + MessageMe[1] + "' , '0')";
                    SqliteCommand command = new SqliteCommand(OhHey, m_dbConnection);
                    command.ExecuteNonQuery();
                }
                else
                {
                    SendTextMessageToUser("<b><span style=\"color:#ff0000\">Error: '!' Must provide a command [try !Help].</span></b>", User);
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

        public void SwitchChannel(MumbleChannel channel)
        {
            SendUserState(channel);

        }

        public MumbleChannel FindChannel(string name)
        {
            return channels.Values.Where(channel => channel.Name == name).FirstOrDefault();
        }

        public MumbleChannel FindChannelID(uint name)
        {
            return channels.Values.Where(channel => channel.ID == name).FirstOrDefault();
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
    }
}
