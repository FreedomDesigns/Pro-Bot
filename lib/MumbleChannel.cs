using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Protocol.Mumble
{

    public class MumbleChannel
    {
        #region Vars

        private MumbleClient client;

        private MumbleChannel parentChannel;

        private List<MumbleChannel> subChannels = new List<MumbleChannel>();
        public ReadOnlyCollection<MumbleChannel> SubChannels
        {
            get
            {
                return new ReadOnlyCollection<MumbleChannel>(subChannels);
            }
        }

        private List<MumbleUser> users = new List<MumbleUser>();
        public ReadOnlyCollection<MumbleUser> Users
        {
            get
            {
                return new ReadOnlyCollection<MumbleUser>(users);
            }
        }

        public string Name { get; private set; }
        public uint ID { get; private set; }

        #endregion

        #region Mumble Channel

        public MumbleChannel(MumbleClient client, ChannelState message)
        {
            this.client = client;

            try
            {
                ID = message.channel_id;
                Name = message.name;

                client.Channels.Add(ID, this);
                client.Channels.TryGetValue(message.parent, out parentChannel);
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Fatal error has occurred - 1893: {0}", e));
            }

            if (IsRoot())
            {
                client.RootChannel = this;
            }
            else
            {
                try
                {
                    parentChannel.subChannels.Add(this);
                }
                catch (Exception e)
                {
                    Console.WriteLine(string.Format("Fatal error has occurred - 1092: {0}", e));
                }
            }
        }

        #endregion

        #region IsRoot

        public bool IsRoot()
        {
            return this == parentChannel;
        }

        #endregion

        #region Update

        public void Update(ChannelState message)
        {

        }

        #endregion

        #region Add and Remove user from Users

        internal void AddLocalUser(MumbleUser user)
        {
            try
            {
                users.Add(user);
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Fatal error has occurred: {0}", e));
            }
        }

        internal void RemoveLocalUser(MumbleUser user)
        {
            try
            {
                users.Remove(user);
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Fatal error has occurred: {0}", e));
            }
        }

        #endregion

        #region Tree

        public string Tree(int level = 0)
        {
            string result = new String(' ', level) + "C " + Name + " (" + ID + ")" + Environment.NewLine;

            foreach (var channel in subChannels)
            {
                result += channel.Tree(level + 1);
            }

            foreach (var user in users)
            {
                result += user.Tree(level + 1);
            }

            return result;
        }

        #endregion
    }
}
