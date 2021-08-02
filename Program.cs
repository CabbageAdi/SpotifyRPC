using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using DiscordRPC;

namespace SpotifyRPC
{
    static class Program
    {
        static event EventHandler<UpdateActivityEventArgs> UpdateStatus;
        static event EventHandler IdleStatus;
        static event EventHandler ClearStatus;

        static bool running;

        static void UpdateActivity(this DiscordRpcClient discord, string songname, string artist)
        {
            try
            {
                discord.SetPresence(new RichPresence
                {
                    Details = songname,
                    State = artist,
                    Timestamps = new Timestamps()
                    {
                        Start = DateTime.UtcNow
                    },
                    Assets = new Assets()
                    {
                        LargeImageKey = "spotify",
                        LargeImageText = "github.com/IDoEverything/SpotifyRPC"
                    }
                });

                Console.WriteLine("updated activity");
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        static void IdleActivity(this DiscordRpcClient discord)
        {
            try
            {
                discord.SetPresence(new RichPresence
                {
                    Details = "Idle",
                    Assets = new Assets()
                    {
                        LargeImageKey = "spotify",
                        LargeImageText = "github.com/IDoEverything/SpotifyRPC"
                    }
                });

                Console.WriteLine("idle");
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        static void Main()
        {
            running = true;
            Run();

            Task.Run(async () =>
            {
                string prevtitle = null;
                bool notPlaying = false;
                while (true)
                {
                    var spotifyProcess = Process.GetProcessesByName("Spotify").Where(x => x.MainWindowTitle != null).FirstOrDefault();
                    if (spotifyProcess != default)
                    {
                        if (!running)
                        {
                            running = true;
                            Run();
                        }
                        if ((spotifyProcess.MainWindowTitle == "Spotify Premium" || spotifyProcess.MainWindowTitle == "Spotify") && notPlaying == false)
                        {
                            IdleStatus?.Invoke(null, new EventArgs());
                            notPlaying = true;
                        }
                        else if (spotifyProcess.MainWindowTitle != prevtitle)
                        {
                            string artist = null;
                            string song = null;
                            if (spotifyProcess.MainWindowTitle.Contains(" - "))
                            {
                                var split = spotifyProcess.MainWindowTitle.Split(" - ");
                                artist = split[0];
                                song = split[1];
                            }
                            else
                            {
                                song = spotifyProcess.MainWindowTitle;
                            }

                            UpdateStatus?.Invoke(null, new UpdateActivityEventArgs { Song = song, Artist = artist });
                            prevtitle = spotifyProcess.MainWindowTitle;
                            notPlaying = false;
                        }
                    }
                    else
                    {
                        ClearStatus?.Invoke(null, new EventArgs());
                    }
                    await Task.Delay(500);
                }
            }).GetAwaiter().GetResult();
        }

        public static async void Run()
        {
            var discord = new DiscordRpcClient("809056398939783249");
            discord.Initialize();
            bool clear = false;

            UpdateStatus += UpdateStatusHandler;
            IdleStatus += IdleStatusHandler;
            ClearStatus += ClearStatusHandler;

            while (!clear)
            {
                await Task.Delay(1000);
            }

            void UpdateStatusHandler(object sender, UpdateActivityEventArgs e)
            {
                if (!clear)
                    discord.UpdateActivity(e.Song, e.Artist);
            }

            void IdleStatusHandler(object sender, EventArgs e)
            {
                if (!clear)
                    discord.IdleActivity();
            }

            void ClearStatusHandler(object sender, EventArgs e)
            {
                discord.ClearPresence();
                clear = true;
                running = false;
                discord.Dispose();
                UpdateStatus -= UpdateStatusHandler;
                IdleStatus -= IdleStatusHandler;
                ClearStatus -= ClearStatusHandler;
            }
        }
    }

    public class UpdateActivityEventArgs
    {
        public string Artist;
        public string Song;
    }
}