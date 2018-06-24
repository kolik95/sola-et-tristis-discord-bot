﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;

namespace MusicBot
{
    public class Commands : ModuleBase<SocketCommandContext>
    {        
        private static readonly ConcurrentDictionary<ulong, ServerProperties> _serverProperties = 
            new ConcurrentDictionary<ulong, ServerProperties>();


        #region Commands
        
        [Command("Test")]
        public async Task A()
        {
            
            await Context.Channel.SendMessageAsync("Test");
            
        }

        [Command("play", RunMode = RunMode.Async)]
        public async Task JoinChannel(string link)
        {

            IAudioClient audioClient;

            if (_serverProperties.ContainsKey(Context.Guild.Id))
            {

                if (_serverProperties[Context.Guild.Id].ConnectedChannel != null)
                    audioClient = _serverProperties[Context.Guild.Id].ConnectedChannel;
                
                else
                    audioClient = await Join();

            }

            else
            {
                               
                audioClient = await Join();
                
                _serverProperties.TryAdd(Context.Guild.Id, 
                    new ServerProperties(false, false, new List<string>(), audioClient ));

            }
            
            _serverProperties[Context.Guild.Id].SetChannel(audioClient);
            
            var url = GetStreamUrl(link);

            string streamUrl = url.StandardOutput.ReadLine();
            
            _serverProperties[Context.Guild.Id].Queue.Add(streamUrl);

            await Context.Channel.SendMessageAsync("Added to queue.");

            await PlayQueue(Context.Guild);

        }

        [Command("leave", RunMode = RunMode.Async)]
        public async Task LeaveChannel()
        {
            IGuild guild = Context.Guild;

            await Leave(guild);

        }
        
        [Command("repeat", RunMode = RunMode.Async)]
        public async Task Repeat()
        {

            if (_serverProperties[Context.Guild.Id].Repeat)
            {

                _serverProperties[Context.Guild.Id].SetRepeat(false);

                await Context.Channel.SendMessageAsync("Repeating off");

            }
            
            else
            {

                _serverProperties[Context.Guild.Id].SetRepeat(true);

                await Context.Channel.SendMessageAsync("Repeating on");

            }
        }

        [Command("skip", RunMode = RunMode.Async)]
        public async Task Skip()
        {
            
            
            
        }
        
        #endregion

        
        #region Processes
        
        private Process CreateStream(string path)
        {
            var ffmpeg = new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            return Process.Start(ffmpeg);
        }

        private Process GetStreamUrl(string path)
        {
            var yt = new ProcessStartInfo
            {
                FileName = "youtube-dl.exe",
                Arguments = $"-f bestaudio -g \"{path}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            return Process.Start(yt);
        }

        #endregion
        
        #region Utils
        
        private async Task SendAsync(IAudioClient client, string path)
        {
            
            var ffmpeg = CreateStream(path);

            ffmpeg.EnableRaisingEvents = true;

            ffmpeg.Exited += StreamEnded;
            
            var output = ffmpeg.StandardOutput.BaseStream;
            
            var discord = client.CreatePCMStream(AudioApplication.Mixed);
            
            await output.CopyToAsync(discord);
            
            await discord.FlushAsync();
            
        }

        private async Task<IAudioClient> Join()
        {
            var target = ((IVoiceState) Context.Message.Author).VoiceChannel;

            IGuild guild = Context.Guild;

            IAudioClient client;
            
            if (target.Guild.Id != guild.Id) return null;

            var audioClient = await target.ConnectAsync();

            _serverProperties[guild.Id].SetChannel(audioClient);
            
            await Context.Channel.SendMessageAsync("Connected to voice.");

            return audioClient;
        }

        private async Task PlayQueue(IGuild guild)
        {
            
            if (_serverProperties[guild.Id].ConnectedChannel == null) return;
            
            if (_serverProperties[guild.Id].Queue.Count == 0)
            {

                await Leave(guild);
                
                return;
                
            }

            if (_serverProperties[guild.Id].Playing == false)
            {
                
                Console.WriteLine("Playing");
                
                _serverProperties[guild.Id].SetPlaying(true);
                
                await SendAsync(_serverProperties[guild.Id].ConnectedChannel , _serverProperties[guild.Id].Queue[0]);              

            }    
        }

        private void StreamEnded(object sender, EventArgs e)
        {
            
            _serverProperties[Context.Guild.Id].SetPlaying(false);

            if(!_serverProperties[Context.Guild.Id].Repeat)
               _serverProperties[Context.Guild.Id].Queue.RemoveAt(0);
            
            PlayQueue(Context.Guild);

        }
        
        private async Task Leave(IGuild guild)
        {
            
            if (_serverProperties[guild.Id].ConnectedChannel == null) return;
            
            IAudioClient client = _serverProperties[guild.Id].ConnectedChannel;

            _serverProperties[guild.Id].Queue.Clear();

            _serverProperties[guild.Id].SetChannel(null);
            
            await Context.Channel.SendMessageAsync("Leaving voice.");
            
            _serverProperties[guild.Id].SetPlaying(false);

            await client.StopAsync();
    
        }
        
        #endregion
        
    }           
}