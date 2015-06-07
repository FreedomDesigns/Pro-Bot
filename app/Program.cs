using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Protocol.Mumble;
using System.Reflection;
using Mono.Data.Sqlite;
using System.Xml;
using System.Xml.Linq;

namespace Mumble.net.app
{
    class Program
    {
        #region Ping Host

        public static bool PingHost(string _HostURI, int _PortNumber)
        {
            try
            {
                TcpClient client = new TcpClient(_HostURI, _PortNumber);
                client.SendTimeout = 1000;
                Console.WriteLine("Success Pinging Server");
                return true;
            }
            catch (Exception)
            {
                Console.WriteLine(string.Format("Error pinging host - {0}:{1}", _HostURI, _PortNumber.ToString()));
                return false;
            }
        }

        #endregion

        static void Main(string[] args)
        {
            DateTime buildDate = new FileInfo(Assembly.GetExecutingAssembly().Location).LastWriteTime;
            string BuildNumber = "3";
            string Version = (string.Format("Apollo 1.0-{0}", BuildNumber));
            string Host = "absy.ddns.net"; // Absys
            //string Host = "192.168.1.23"; // Localhost
            string BotName = "mmblBot";
            string DB = @"XNP.Sqlite";

            Console.Title = string.Format("Freedoms Mumble Bot - {0}", Version);

            #region Console Header

            // Console header
            Console.WriteLine("+-------------------------------------+");
            Console.WriteLine("| Freedom's Mumble Bot!               |");
            Console.WriteLine("+-------------------------------------+");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("{0} built on {1}", Version, buildDate);
            Console.WriteLine("Running on {0}", Environment.OSVersion.ToString());
            Console.ResetColor();
            Console.WriteLine("+-------------------------------------+");

            #endregion

            #region Database Check And Creator

            // Database creator
            if (!File.Exists(DB))
            {
                Console.WriteLine("No Database Found, Creating New File");
                SqliteConnection.CreateFile(DB);
                Console.WriteLine("Created Database File.");

                Console.WriteLine("Filling Database.");
                SqliteConnection m_dbConnection;
                m_dbConnection = new SqliteConnection("Data Source=" + DB + ";Version=3;");
                m_dbConnection.Open();

                string sql = "";

                sql = "CREATE TABLE `Users` ( `ID`	INTEGER PRIMARY KEY AUTOINCREMENT, `Name`	varchar(20) DEFAULT 'Anonymous', `LastSeen`	varchar(20) DEFAULT '00/00/00 00:00:00',`Online`	NUMERIC DEFAULT 0,`Actor`	varchar(20) DEFAULT 0,`Session`	varchar(20) DEFAULT 0)";
                SqliteCommand command = new SqliteCommand(sql, m_dbConnection);
                command.ExecuteNonQuery();
                sql = "CREATE TABLE `Messages` (`ID`	INTEGER PRIMARY KEY AUTOINCREMENT,`To`	varchar(20),`From`	varchar(20),`Message`	varchar(50),`Recived`	NUMERIC)";
                SqliteCommand cmd = new SqliteCommand(sql, m_dbConnection);
                cmd.ExecuteNonQuery();
                Console.WriteLine("Database Complete.");
            }

            #endregion

            #region Check Server Availability

            Boolean Ping = PingHost(Host, 64738);

            if (Ping != true)
            {
                Console.WriteLine("Press any key to continue!");
                Console.ReadLine();
                Environment.Exit(0);
            }

            #endregion

            #region Connection

            Console.WriteLine("Connecting ...");

            var client = new MumbleClient("1.2.0", Host, BotName);

            Console.WriteLine(string.Format("Connected As: {0}", BotName));

            client.DoIKnowYou(DB, Version);

            client.Connect();

            #endregion

            #region Event Loop

            /* This keeps the program running */
            for (; ; )
            {
                Console.ReadLine();
            }

            #endregion

        }
    }
}
