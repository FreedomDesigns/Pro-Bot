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

        public string DB = @"XNP.Sqlite";

        public MumbleUser(MumbleClient client, UserState message)
        {
            this.client = client;
            Name = message.name;
            Session = message.session;

            client.Users.Add(Session, this);

            Channel = client.Channels[message.channel_id];

            Channel.AddLocalUser(this);
            DateTime NOW = DateTime.Now;
            Console.WriteLine(NOW + " - " + Name + " Joined Server");

            SqliteConnection m_dbConnection;
            m_dbConnection = new SqliteConnection("URI=file:" + DB + ",version=3");
            m_dbConnection.Open();

            string Exists = "SELECT * FROM `Users` WHERE `Name`='" + Name.ToUpper() + "'";
            SqliteCommand CMD = new SqliteCommand(Exists, m_dbConnection);
            CMD.ExecuteNonQuery();

            me(message.session);

            string online = "";
            string heremate = "";

            int count = Convert.ToInt32(CMD.ExecuteScalar());
            if (count == 0)
            {
                online = "INSERT INTO Users (Name, Online, Actor, Session) VALUES ('" + Name.ToUpper() + "', '1', '" + message.actor + "', '" + message.session + "')";
            }
            else
            {
                online = "UPDATE `Users` SET `Online`='1', `Actor`='" + message.actor + "', `Session`='" + message.session + "' WHERE `Name`='" + Name.ToUpper() + "'";
                heremate = "UPDATE `Messages` SET `Recived`='1' WHERE `To`='" + Name.ToUpper() + "' AND `Recived`='0'";
            }

            SqliteCommand command = new SqliteCommand(online, m_dbConnection);
            command.ExecuteNonQuery();
            SqliteCommand NewMw = new SqliteCommand(heremate, m_dbConnection);
            NewMw.ExecuteNonQuery();
            System.Threading.Thread.Sleep(100);
        }

        public void me(uint actor)
        {

            var User = client.FindUser(actor);
            SqliteConnection m_dbConnection;
            m_dbConnection = new SqliteConnection("Data Source=" + DB + ";Version=3;");
            m_dbConnection.Open();

            

            string Messagez = "SELECT * FROM `Messages` WHERE `To`='" + Name.ToUpper() + "' AND `Recived`='0'";
            SqliteCommand NewMate = new SqliteCommand(Messagez, m_dbConnection);
            NewMate.ExecuteNonQuery();

            using (SqliteDataReader rdr = NewMate.ExecuteReader())
            {
                while (rdr.Read())
                {
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
            string DB = @"XNP.Sqlite";
            string Last = "";
            DateTime NOW = DateTime.Now;

            client.Channels.Remove(Session);
            Channel.RemoveLocalUser(this);
            Console.WriteLine(NOW + " - " + Name + " Left server");

            SqliteConnection m_dbConnection;
            m_dbConnection = new SqliteConnection("Data Source=" + DB + ";Version=3;");
            m_dbConnection.Open();

            string Exists = "SELECT * FROM `Users` WHERE `Name`='" + Name.ToUpper() + "'";
            SqliteCommand CMD = new SqliteCommand(Exists, m_dbConnection);
            CMD.ExecuteNonQuery();

            Last = "UPDATE `Users` SET `LastSeen`='" + NOW + "', `Online`='0' WHERE `Name`='" + Name.ToUpper() + "'";

            SqliteCommand command = new SqliteCommand(Last, m_dbConnection);
            command.ExecuteNonQuery();

        }

        public string Tree(int level)
        {
            return new String(' ', level) + "U " + Name + " (" + Session + ")" + Environment.NewLine;
        }

    }
}
