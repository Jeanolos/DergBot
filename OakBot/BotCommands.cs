using Discord;
using System;
using System.Linq;

namespace OakBot
{
    public class BotCommands
    {
        #region Public Methods

        public async static void RunBotCommand(string command, IrcMessage message)
        {
            // !quote > display random quote
            // !quote # > display quote of given id
            // !quote add quote - quoter > adds quote
            // !quote remove # > removes quote by given id
            // If given Id is int and not found, display not exists
            // If given Id is not int, dont display

            Viewer sender = MainWindow.colViewers.First(x => x.UserName.ToLower() == message.Author.ToLower());

            if (command == "!quote")
            {
                string[] splitMessage = message.Message.Split(new char[] { ' ' }, 3);

                if (splitMessage.Count() >= 2 && splitMessage[1].ToLower() == "add")
                {
                    try
                    {
                        // Split quote and quoter on -
                        string[] splitEntry = splitMessage[2].Split('-');

                        // Create new quote with game that the streamer on channel is/was playing
                        Quote newQuote = new Quote(splitEntry[0].Trim(), splitEntry[1].Trim(),
                            Utils.GetClient().GetMyChannel().Game);

                        // Add new quote to collection
                        App.Current.Dispatcher.BeginInvoke(new Action(delegate
                        {
                            MainWindow.colQuotes.Add(newQuote);
                        }));

                        // Save to database
                        DatabaseUtils.AddQuote(newQuote);

                        // Send response
                        SendAndShowMessage(string.Format("Quote has been added with ID of: {0}", newQuote.Id));
                    }
                    catch (Exception)
                    {
                        SendAndShowMessage("To add a quote use: !quote add <quote> - <quoter> No need to use \" as it will be added on display.");
                    }
                }
                else if (splitMessage.Count() >= 2 && splitMessage[1].ToLower() == "remove")
                {
                    try
                    {
                        int idToRemove = int.Parse(splitMessage[2]);

                        if (idToRemove < MainWindow.colQuotes.Count())
                        {
                            // Remove quote from collection
                            App.Current.Dispatcher.BeginInvoke(new Action(delegate
                            {
                                MainWindow.colQuotes.RemoveAt(idToRemove);
                            }));

                            // Update whole database file (dynamic id)
                            DatabaseUtils.SaveAllQuotes();

                            // Send response
                            SendAndShowMessage("Quote removed with id: " + splitMessage[2]);
                        }
                        else
                        {
                            // Send response
                            SendAndShowMessage("The quote with the given id does not exist.");
                        }
                    }
                    catch
                    {
                        SendAndShowMessage("Given id is not a valid id number");
                    }
                }
                else
                {
                    Quote q;

                    try
                    {
                        // Try to get quote from given id
                        q = MainWindow.colQuotes[int.Parse(splitMessage[1])];
                    }
                    catch
                    {
                        // Get a random quote if arg is not parsable or out of range
                        Random rnd = new Random((int)DateTime.Now.Ticks);
                        q = MainWindow.colQuotes[rnd.Next(0, MainWindow.colQuotes.Count)];
                    }

                    // Send response
                    SendAndShowMessage(string.Format("Quote #{0}: \"{1}\" - {2} {3} {4}",
                        q.Id,
                        q.QuoteString, q.Quoter,
                        q.DisplayDate ? "[" + q.DateString + "]" : "",
                        q.DisplayGame ? "while playing " + q.Game : "")
                    );
                }
            }
            else if (command == "!songrequest")
            {
                string[] splitMessage = message.Message.Split(new char[] { ' ' }, 2);

                if (splitMessage.Count() > 1)
                {
                    Song requestedSong = new Song(splitMessage[1]);
                    if (requestedSong.Type != SongType.INVALID)
                    {
                        // Add to colSongs
                        App.Current.Dispatcher.BeginInvoke(new Action(delegate
                        {
                            MainWindow.colSongs.Add(requestedSong);
                        }));

                        // Display response
                        SendAndShowMessage("The following song has been added: " + Utils.getTitleFromYouTube(splitMessage[1]));
                    }
                    else
                    {
                        // Display response
                        SendAndShowMessage("Invalid song link or id.");
                    }
                }
            }
            else if (command == "!currentsong")
            {
                if (MainWindow.playState)
                {
                    SendAndShowMessage("Current song playing: " + MainWindow.colSongs[MainWindow.indexSong].SongName);
                }
            }
            else if (command == "!nextsong")
            {
                if (MainWindow.playState)
                {
                    MainWindow.instance.nextSong();
                }
            }
            else if (command == "!prevsong")
            {
                if (MainWindow.playState)
                {
                    MainWindow.instance.prevSong();
                }
            }
            else if (command == "!volume")
            {
                string[] splitMessage = message.Message.Split(' ');
                if(splitMessage.Length == 2)
                {
                    MainWindow.instance.cefSong.Load("javascript:var mv = document.getElementById('movie_player'); mv.setVolume(" + splitMessage[1] + "); ");
                    MainWindow.instance.lblVol.Content = splitMessage[1];
                    SendAndShowMessage("Set volume to " + splitMessage[1]);
                }else
                {
                    SendAndShowMessage("Volume is currenty at " + MainWindow.instance.lblVol.Content);
                }
            }
            else if (command == "!link") {
                string[] splitMessage = message.Message.Split(new char[] { ' ' }, 2);
                if (splitMessage.Count() != 2)
                {
                    SendAndShowMessage("In order to link your Twitch name to this bot, you have to get your Discord User ID. You can get that by using !id in Discord. Then, proceed to use !link [id] here in chat. Further instructions are following in a Discord message!");
                } else
                {
                    Channel priv;
                    ulong dummy = 0;
                    if (ulong.TryParse(splitMessage[1], out dummy))
                    {
                        priv = MainWindow.discord.CreatePrivateChannel(ulong.Parse(splitMessage[1])).Result;
                    }else
                    {
                        priv = null;
                    }
                    if (priv.IsPrivate && priv != null)
                    {
                        string msg = string.Format(@"Hello!

The user {0} wants to link his Twitch account with your Discord name!

**If you didn't use !link {1} in the Twitch chat, please ignore this message!** Else, please do the following steps:

**1.** Change your Twitch game to the ID (**{1}**) you used with !link
**2.** Reply with `confirm {0}` (case sensitive)
**3.** Test a user specific command (like !followdate) in the Discord chat", sender.UserName, splitMessage[1]);
                        priv.SendMessage(msg);
                    }
                }
            }
        }

        public static void RunBotCommandDiscord(string command, Message message)
        {
            Viewer sender = new Viewer(message.User.Name);

            if (command == "!quote")
            {
                string[] splitMessage = message.Text.Split(new char[] { ' ' }, 3);

                if (splitMessage.Count() >= 2 && splitMessage[1].ToLower() == "add")
                {
                    try
                    {
                        // Split quote and quoter on -
                        string[] splitEntry = splitMessage[2].Split('-');

                        // Create new quote with game that the streamer on channel is/was playing
                        Quote newQuote = new Quote(splitEntry[0].Trim(), splitEntry[1].Trim(),
                            Utils.GetClient().GetMyChannel().Game);

                        // Add new quote to collection
                        App.Current.Dispatcher.BeginInvoke(new Action(delegate
                        {
                            MainWindow.colQuotes.Add(newQuote);
                        }));

                        // Save to database
                        DatabaseUtils.AddQuote(newQuote);

                        // Send response
                        SendMessageDiscord(string.Format("Quote has been added with ID of: {0}", newQuote.Id), message.Server.Id, message.Channel.Id);
                    }
                    catch (Exception)
                    {
                        SendMessageDiscord("To add a quote use: !quote add <quote> - <quoter> No need to use \" as it will be added on display.", message.Server.Id, message.Channel.Id);
                    }
                }
                else if (splitMessage.Count() >= 2 && splitMessage[1].ToLower() == "remove")
                {
                    try
                    {
                        int idToRemove = int.Parse(splitMessage[2]);

                        if (idToRemove < MainWindow.colQuotes.Count())
                        {
                            // Remove quote from collection
                            App.Current.Dispatcher.BeginInvoke(new Action(delegate
                            {
                                MainWindow.colQuotes.RemoveAt(idToRemove);
                            }));

                            // Update whole database file (dynamic id)
                            DatabaseUtils.SaveAllQuotes();

                            // Send response
                            SendMessageDiscord("Quote removed with id: " + splitMessage[2], message.Server.Id, message.Channel.Id);
                        }
                        else
                        {
                            // Send response
                            SendMessageDiscord("The quote with the given id does not exist.", message.Server.Id, message.Channel.Id);
                        }
                    }
                    catch
                    {
                        SendMessageDiscord("Given id is not a valid id number", message.Server.Id, message.Channel.Id);
                    }
                }
                else
                {
                    Quote q;

                    try
                    {
                        // Try to get quote from given id
                        q = MainWindow.colQuotes[int.Parse(splitMessage[1])];
                    }
                    catch
                    {
                        // Get a random quote if arg is not parsable or out of range
                        Random rnd = new Random((int)DateTime.Now.Ticks);
                        q = MainWindow.colQuotes[rnd.Next(0, MainWindow.colQuotes.Count)];
                    }

                    // Send response
                    SendMessageDiscord(string.Format("Quote #{0}: \"{1}\" - {2} {3} {4}",
                        q.Id,
                        q.QuoteString, q.Quoter,
                        q.DisplayDate ? "[" + q.DateString + "]" : "",
                        q.DisplayGame ? "while playing " + q.Game : "")
                    , message.Server.Id, message.Channel.Id);
                }
            }
            else if (command == "!songrequest")
            {
                string[] splitMessage = message.Text.Split(new char[] { ' ' }, 2);

                if (splitMessage.Count() > 1)
                {
                    Song requestedSong = new Song(splitMessage[1]);
                    if (requestedSong.Type != SongType.INVALID)
                    {
                        // Add to colSongs
                        App.Current.Dispatcher.BeginInvoke(new Action(delegate
                        {
                            MainWindow.colSongs.Add(requestedSong);
                        }));

                        // Display response
                        SendMessageDiscord("The following song has been added: " + Utils.getTitleFromYouTube(splitMessage[1]), message.Server.Id, message.Channel.Id);
                    }
                    else
                    {
                        // Display response
                        SendMessageDiscord("Invalid song link or id.", message.Server.Id, message.Channel.Id);
                    }
                }
            }
            else if (command == "!currentsong")
            {
                if (MainWindow.playState)
                {
                    SendMessageDiscord("Current song playing: " + MainWindow.colSongs[MainWindow.indexSong].SongName, message.Server.Id, message.Channel.Id);
                }
            }
            else if (command == "!nextsong")
            {
                if (MainWindow.playState)
                {
                    MainWindow.instance.nextSong();
                }
            }
            else if (command == "!prevsong")
            {
                if (MainWindow.playState)
                {
                    MainWindow.instance.prevSong();
                }
            }
            else if(command == "!id")
            {
                SendMessageDiscord(string.Format("{0} Your ID is {1}", message.User.Mention, message.User.Id), message.Server.Id, message.Channel.Id);
            }
            else if(command == "!twitchname")
            {
                Viewer vwr = MainWindow.colDatabase.FirstOrDefault(x => x.DiscordID == message.User.Id.ToString());
                if(vwr != null)
                {
                    SendMessageDiscord(string.Format("{0}, your Twitch name is **{1}**!", message.User.Mention, vwr.UserName), message.Server.Id, message.Channel.Id);
                }else
                {
                    SendMessageDiscord(string.Format("{0}, you haven't linked your Twitch account yet!"), message.Server.Id, message.Channel.Id);
                }
            }
        }

        #endregion Public Methods

        #region Private Methods

        public static void SendAndShowMessage(string message)
        {
            // Send the message to IRC
            MainWindow.instance.botChatConnection.SendChatMessage(message);

            // Add message to collection
            App.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                MainWindow.colChatMessages.Add(new IrcMessage(
                    MainWindow.instance.accountBot.UserName, message));
            }));
        }

        private static void SendMessageDiscord(string message, ulong serverId, ulong channelId)
        {
            MainWindow.discord.GetServer(serverId).GetChannel(channelId).SendMessage(message);
        }

        #endregion Private Methods
    }
}