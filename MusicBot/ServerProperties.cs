﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Audio;

namespace MusicBot
{
    public struct ServerProperties
    {
        
        public List<string> Queue { get; set; }

        public bool Repeat { get; set; }

        public bool Playing { get; set; }
        
        public IAudioClient ConnectedChannel { get; set; }

        public ServerProperties(bool Repeat, bool Playing, List<string> Queue, IAudioClient ConnectedChannel)
        {
            this.Queue = Queue;
            this.Repeat = Repeat;
            this.Playing = Playing;
            this.ConnectedChannel = ConnectedChannel;

        }

        public void SetRepeat(bool value)
        {

            Repeat = value;

        }
        
        public void SetPlaying(bool value)
        {

            Playing = value;

        }
        
        public void SetChannel(IAudioClient value)
        {

            ConnectedChannel = value;

        }
        
    }
}