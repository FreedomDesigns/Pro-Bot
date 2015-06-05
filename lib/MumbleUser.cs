using System;
using System.IO;
using Mono.Data.Sqlite;

namespace Protocol.Mumble
{
    public class MumbleUser
    {
        private readonly MumbleClient client;

        public MumbleChannel Channel { get; private set; }
        public string Name { get; private set; }
        public uint Session { get; private set; }
        public bool Deaf { get; private set; }
        public bool DeafSelf { get; private set; }
        public bool Mute { get; private set; }
        public bool MuteSelf { get; private set; }

        // Private only vars
        public string DB = @"XNP.Sqlite";
        private string online = "";
        private string messageUpdate = "";
        private string lastTime = "";
        private DateTime NOW = DateTime.Now;

        public MumbleUser(MumbleClient client, UserState message)
        {
            this.client = client;
            Name = message.name;
            Session = message.session;

            client.Users.Add(Session, this);

            Channel = client.Channels[message.channel_id];

            Channel.AddLocalUser(this);

            // Writes user join time and name in console
            Console.WriteLine(NOW + " - " + Name + " Joined Server");

            // Connects to Database
            SqliteConnection m_dbConnection;
            m_dbConnection = new SqliteConnection("URI=file:" + DB + ",version=3");
            m_dbConnection.Open();

            // Query if name exists on User table
            string Exists = "SELECT * FROM `Users` WHERE `Name`='" + Name.ToUpper() + "'";
            SqliteCommand CMD = new SqliteCommand(Exists, m_dbConnection);
            CMD.ExecuteNonQuery();

            // Opens new message thread
            CheckMessages(message.session);

            //Converts Exists qeury into int [E.G 0 or 1+]
            int count = Convert.ToInt32(CMD.ExecuteScalar());
            if (count == 0)
            {
                // --- User does not exist in database --- //
                online = "INSERT INTO Users (Name, Online, Actor, Session) VALUES ('" + Name.ToUpper() + "', '1', '" + message.actor + "', '" + message.session + "')";
            }
            else
            {
                // --- User does exist in database --- //
                online = "UPDATE `Users` SET `Online`='1', `Actor`='" + message.actor + "', `Session`='" + message.session + "' WHERE `Name`='" + Name.ToUpper() + "'";
                messageUpdate = "UPDATE `Messages` SET `Recived`='1' WHERE `To`='" + Name.ToUpper() + "' AND `Recived`='0'";
            }

            // Runs the insert and update commands above into database
            SqliteCommand command = new SqliteCommand(online, m_dbConnection);
            command.ExecuteNonQuery();
            SqliteCommand NewMw = new SqliteCommand(messageUpdate, m_dbConnection);
            NewMw.ExecuteNonQuery();

            // This wait is need to allow time for the user to be process correctly or all hell breaks lose
            System.Threading.Thread.Sleep(100);
        }

        public void CheckMessages(uint actor)
        {
            // Gets user ID for messaging 
            var User = client.FindUser(actor);

            // Connects to database
            SqliteConnection m_dbConnection;
            m_dbConnection = new SqliteConnection("Data Source=" + DB + ";Version=3;");
            m_dbConnection.Open();

            // Query's database for if user has any messages
            string Messagez = "SELECT * FROM `Messages` WHERE `To`='" + Name.ToUpper() + "' AND `Recived`='0'";
            SqliteCommand NewMate = new SqliteCommand(Messagez, m_dbConnection);
            NewMate.ExecuteNonQuery();

            // Reads all results from query
            using (SqliteDataReader rdr = NewMate.ExecuteReader())
            {
                while (rdr.Read())
                {
                    // Sends message to user with all required information
                    client.SendTextMessageToUser("<b>" + rdr["From"] + " - " + rdr["Message"] + " </b>", User);
                }
            }
        }

        public void Update(UserState message)
        {
            if (message.channel_idSpecified && message.channel_id != Channel.ID)
            {
                Channel.RemoveLocalUser(this);
                Channel = client.Channels[message.channel_id];
                Channel.AddLocalUser(this);
            }

            if (message.deafSpecified) { Deaf = message.deaf; }
            if (message.self_deafSpecified) { DeafSelf = message.self_deaf; }
            if (message.muteSpecified) { Mute = message.mute; }
            if (message.self_muteSpecified) { MuteSelf = message.self_mute; }
        }

        public void Update(UserRemove message)
        {        
            client.Channels.Remove(Session);
            Channel.RemoveLocalUser(this);

            // Writes user leave time and name in console
            Console.WriteLine(NOW + " - " + Name + " Left server");

            // Connects to database
            SqliteConnection m_dbConnection;
            m_dbConnection = new SqliteConnection("Data Source=" + DB + ";Version=3;");
            m_dbConnection.Open();

            lastTime = "UPDATE `Users` SET `LastSeen`='" + NOW + "', `Online`='0' WHERE `Name`='" + Name.ToUpper() + "'";

            // Runs update command for database
            SqliteCommand command = new SqliteCommand(lastTime, m_dbConnection);
            command.ExecuteNonQuery();
        }

        public string Tree(int level)
        {
            return new String(' ', level) + "U " + Name + " (" + Session + ")" + Environment.NewLine;
        }

    }
}
