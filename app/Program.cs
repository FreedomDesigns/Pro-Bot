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

namespace Mumble.net.app
{
    class Program
    {
        static void Main(string[] args)
        {

            DateTime buildDate = new FileInfo(Assembly.GetExecutingAssembly().Location).LastWriteTime;
            string BUILDNUMBER_STR = "1";
            string VERSIONSTRING = ("Zero 1.0-" + BUILDNUMBER_STR);
            string Host = "absy.ddns.net";
            string BotName = "ProBot";
            string DB = @"XNP.Sqlite";


            // Console header
            Console.WriteLine("+-------------------------------------+");
            Console.WriteLine("| Freedom's Mumble Bot!                |");
            Console.WriteLine("+-------------------------------------+");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("{0} built on {1}", VERSIONSTRING, buildDate);
            Console.WriteLine("http://FreedomTS.tk");
            Console.ResetColor();
            Console.WriteLine("+-------------------------------------+");
            //Console.WriteLine("Enter a command you want to RCON!");
            //Console.WriteLine("For help and some simple commands type '!help'");
            //Console.WriteLine("+----------------------------------------------+");

            Console.WriteLine("Connecting ...");

            var client = new MumbleClient("1.2.0", Host, BotName);

            client.Connect();

            Console.Title = Host + "- " + BotName;

            Console.WriteLine("Connected As: ProBot");

            // LastSeen Txt creator
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

            for (; ; )
            {
                string commandz = Console.ReadLine();

                if (commandz == "" || commandz != "")
                {
                }
            }

        }
    }
}
