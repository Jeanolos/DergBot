using Discord;
using OakBot.Args;
using OakBot.Clients;
using OakBot.Models;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;

// http://stackoverflow.com/questions/2505492/wpf-update-binding-in-a-background-thread
// http://stackoverflow.com/questions/2006729/how-can-i-have-a-listbox-auto-scroll-when-a-new-item-is-added
// >> http://stackoverflow.com/questions/12255055/how-to-get-itemscontrol-scrollbar-position-programmatically

namespace OakBot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        #region Public Fields

        public static ObservableCollection<UserCommand> colBotCommands = new ObservableCollection<UserCommand>();

        // Collections
        public static ObservableCollection<IrcMessage> colChatMessages = new ObservableCollection<IrcMessage>();

        public static ObservableCollection<WindowViewerChat> colChatWindows = new ObservableCollection<WindowViewerChat>();

        public static ObservableCollection<Viewer> colDatabase = new ObservableCollection<Viewer>();

        public static ObservableCollection<Quote> colQuotes = new ObservableCollection<Quote>();

        public static ObservableCollection<Song> colSongs = new ObservableCollection<Song>();

        public static ObservableCollection<Viewer> colViewers = new ObservableCollection<Viewer>();

        public static ObservableCollection<Giveaway> colGiveaways = new ObservableCollection<Giveaway>();

        public static DiscordClient discord;

        public static int indexSong = -1;

        // Instance of itself
        public static MainWindow instance;

        // Song Info
        public static bool playState = false;

        public TwitchCredentials accountBot;

        // Streamer and Bot account info
        public TwitchCredentials accountStreamer;

        // Chat connections
        public TwitchChatConnection botChatConnection;

        public TwitchChatConnection streamerChatConnection;

        public Giveaway testGw, testGw2;

        #endregion Public Fields

        #region Private Fields

        private object _lockChat = new object();

        private object _lockDatabase = new object();

        private object _lockSongs = new object();

        private object _lockViewers = new object();

        private object _lockGiveaways = new object();

        private Thread botChat;

        // API Client
        private TwitchAuthenticatedClient client;

        private ICollectionView databaseView;

        // Threads
        private Thread streamerChat;

        #endregion Private Fields

        #region Public Constructors

        public MainWindow()
        {
            InitializeComponent();

            discord = new DiscordClient(x =>
            {
                x.AppName = "OakBot";
                x.AppUrl = "http://github.com/ocgineer/OakBot";
                x.AppVersion = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion;
                x.UsePermissionsCache = false;
            });


            // Initialize instance
            instance = this;

            this.DataContext = this;

            if (!Directory.Exists(Config.AppDataPath)) Directory.CreateDirectory(Config.AppDataPath);
            if (!Directory.Exists(Config.AppDataPath + "\\Webserver")) Directory.CreateDirectory(Config.AppDataPath + "\\Webserver");

            // Initialize config
            Config.GetConfigFromDb();
            LoadConfigToUI();
            DatabaseUtils.LoadAllViewers();
            DatabaseUtils.LoadAllQuotes();

            // Enable sync between threads
            BindingOperations.EnableCollectionSynchronization(colChatMessages, _lockChat);
            BindingOperations.EnableCollectionSynchronization(colViewers, _lockViewers);
            BindingOperations.EnableCollectionSynchronization(colDatabase, _lockDatabase);
            BindingOperations.EnableCollectionSynchronization(colSongs, _lockSongs);
            BindingOperations.EnableCollectionSynchronization(colGiveaways, _lockGiveaways);

            // Create Event for collection changed
            colChatMessages.CollectionChanged += colChatMessages_Changed;

            // Link listViews with collections
            //listViewChat.ItemsSource = colChatMessages;
            listViewViewers.ItemsSource = colViewers;

            // Database listView with filter
            lvViewerDatabase.ItemsSource = colDatabase;
            databaseView = CollectionViewSource.GetDefaultView(lvViewerDatabase.ItemsSource);
            databaseView.Filter = DatabaseFilter;
            lblFilterCnt.Content = databaseView.Cast<Viewer>().Count();

            lvCommands.ItemsSource = colBotCommands;
            lvQuotes.ItemsSource = colQuotes;

            lvSongs.ItemsSource = colSongs;

            lvGiveaways.ItemsSource = colGiveaways;

            // Testing Commands
            colBotCommands.Add(new UserCommand("!test", "Test received!", 30, 0, true));
            colBotCommands.Add(new UserCommand(":yatb", "Yet Another Twitch Bot.", 30, 60, true));
            colBotCommands.Add(new UserCommand("!who", "You are @user@", 0, 0, true));
            colBotCommands.Add(new UserCommand("!block", "@block@ Hello thur!", 0, 0, true));
            colBotCommands.Add(new UserCommand("!followdate", "@user@, you are following since @followdate@.", 0, 0, true));
            colBotCommands.Add(new UserCommand("!followdatetime", "@user@, you are following since @followdatetime@", 0, 0, true));
            colBotCommands.Add(new UserCommand("!vartest", "@var1@ m8", 0, 0, true));
            colBotCommands.Add(new UserCommand("!song", "Currently playing: @song@!", 0, 0, true));
            colBotCommands.Add(new UserCommand("Giveaway", "Just stop...", 0, 0, true, true));
            colBotCommands.Add(new UserCommand("!slap", "@user@ slaps @target@ so hard, he bursts into pieces!", 0, 0, true));

            colSongs.Add(new Song("https://www.youtube.com/watch?v=VEAy700YGuU"));
            colSongs.Add(new Song("https://soundcloud.com/aivisura/steven-universe-strong-in-the-real-way-rebecca-sugar"));

            string message = "Hello and welcome to the first OakBot Alpha ever to be released! I know it took some time, but I also have a private life, you know? ;)\n\n" +
                             "Anyways, this version has not many functions yet, but the ones that are listed here, are fully working.\n\n" +
                             "WORKING:\n" +
                             "- Twitch Chat\n" +
                             "- Commands\n" +
                             "  - Song Requests (the controls are only working for YouTube at the moment)\n" +
                             "  - Quotes\n" +
                             "  - Follow Date\n" +
                             "- Discord Integration\n" +
                             "- Import from Ankhbot\n" +
                             "- Dashboard and Viewer related stuff\n\n" +
                             "IMPLEMENTED, BUT NOT FULLY WORKING:\n" +
                             "- Giveaways\n" +
                             "- Stream Currency Gain\n" +
                             "- The UI itself\n\n" +
                             "I know, there's much to do, but bear with me. I'm working as hard as I can c:";
            MessageBox.Show(message, "OakBot Alpha 1");

            // BackgroundTask Thread
            BackgroundTasks bg = new BackgroundTasks(60, 120);
            new Thread(new ThreadStart(bg.Run)) { IsBackground = true }.Start();

            // Auto connect
            if (Config.AutoConnectBot)
            {
                ConnectBot();

                // Can only connect if Bot is connected
                if (Config.AutoConnectStreamer)
                {
                    ConnectStreamer();
                }
            }
        }

        #endregion Public Constructors

        #region Public Properties

        // For binding
        public ObservableCollection<IrcMessage> ChatMessages
        {
            get
            {
                return colChatMessages;
            }
        }

        // For binding
        public ObservableCollection<Viewer> Chatters
        {
            get
            {
                return colViewers;
            }
        }

        #endregion Public Properties

        #region Public Methods

        public void DisconnectBot()
        {
            try
            {
                botChat.Abort();
            }
            catch (ThreadAbortException)
            {
            }
            catch (Exception)
            {
            }

            // Enable UI elements
            textBoxBotName.IsEnabled = true;
            buttonBotConnect.IsEnabled = true;
            tbChannelName.IsEnabled = true;
            cbServerIP.IsEnabled = true;
            cbServerPort.IsEnabled = true;
            cbAutoConnectBot.IsEnabled = true;
            btnBotConnect.Content = "Connect";
        }

        public void DisconnectStreamer()
        {
            try
            {
                streamerChat.Abort();
            }
            catch (ThreadAbortException)
            {
            }
            catch (Exception)
            {
            }

            // Enable UI elements
            textBoxStreamerName.IsEnabled = true;
            buttonStreamerConnect.IsEnabled = true;
            cbAutoConnectStreamer.IsEnabled = true;
            btnStreamerConnect.Content = "Connect";
        }

        public void LoadConfigToUI()
        {
            textBoxBotName.Text = Config.BotUsername;
            tbChannelName.Text = Config.ChannelName;
            cbAutoConnectBot.IsChecked = Config.AutoConnectBot;
            textBoxStreamerName.Text = Config.StreamerUsername;
            cbAutoConnectBot.IsChecked = Config.AutoConnectStreamer;
            cbAutoConnectStreamer.IsChecked = Config.AutoConnectStreamer;
            txtToken.Text = Config.DiscordBotToken;
        }

        #endregion Public Methods

        #region Private Methods


        private void btn120Secs_Click(object sender, RoutedEventArgs e)
        {
            RunManualCommercial(120);
        }

        private void btn150Secs_Click(object sender, RoutedEventArgs e)
        {
            RunManualCommercial(150);
        }

        private void btn180Secs_Click(object sender, RoutedEventArgs e)
        {
            RunManualCommercial(180);
        }

        private void btn30Sec_Click(object sender, RoutedEventArgs e)
        {
            RunManualCommercial(30);
        }

        private void btn60Sec_Click(object sender, RoutedEventArgs e)
        {
            RunManualCommercial(60);
        }

        private void btn90Secs_Click(object sender, RoutedEventArgs e)
        {
            RunManualCommercial(90);
        }

        private void btnBotConnect_Click(object sender, RoutedEventArgs e)
        {
            if (btnBotConnect.Content.ToString() == "Connect")
            {
                ConnectBot();
            }
            else
            {
                DisconnectBot();
            }
        }

        private void btnDatabaseCleanup_Click(object sender, RoutedEventArgs e)
        {
            WindowDatabaseCleanup windowCleanup = new WindowDatabaseCleanup();
            windowCleanup.ShowDialog();
        }

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            WindowImportData windowImport = new WindowImportData();
            windowImport.ShowDialog();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            if((string)btnLogin.Content == "Login")
            {
                //https://discordapp.com/oauth2/authorize?client_id=168646630260736000&scope=bot&permissions=267648063
                discord.Connect(txtToken.Text);
                while (discord.State != ConnectionState.Connected)
                {
                    Task.Delay(25);
                }
                discord.MessageReceived += Discord_MessageReceived;
                txtUsername.Text = discord.CurrentUser.Name;
                int serverCounter = 0, channelCounter = 0, userCounter = 0;
                foreach (Server s in discord.Servers)
                {
                    serverCounter++;
                    TreeViewItem server = new TreeViewItem();
                    server.Header = s.Name;
                    TreeViewItem voice = new TreeViewItem(), text = new TreeViewItem();
                    voice.Header = "Voice Channels";
                    text.Header = "Text Channels";
                    foreach (Discord.Channel c in s.VoiceChannels)
                    {
                        channelCounter++;
                        TreeViewItem channel = new TreeViewItem();
                        channel.Header = c.Name;
                        foreach (Discord.User u in c.Users)
                        {
                            userCounter++;
                            channel.Items.Add(u.Name);
                        }
                        voice.Items.Add(channel);
                    }
                    foreach (Discord.Channel c in s.TextChannels)
                    {
                        channelCounter++;
                        text.Items.Add(c);
                    }
                    server.Items.Add(voice);
                    server.Items.Add(text);
                    tvServerBrowser.Items.Add(server);
                }
                btnLogin.Content = "Logout";
            }
            else
            {
                discord.Disconnect();
                tvServerBrowser.Items.Clear();
                btnLogin.Content = "Login";
            }
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            nextSong();
        }

        public void nextSong()
        {
            if (indexSong == colSongs.Count - 1)
            {
                indexSong = 0;
            }
            else
            {
                indexSong++;
            }
            Song play = colSongs[indexSong];
            cefSong.Load(play.Link);
        }

        public void prevSong()
        {
            if (indexSong == 0)
            {
                indexSong = colSongs.Count - 1;
            }
            else
            {
                indexSong--;
            }
            Song play = colSongs[indexSong];
            cefSong.Load(play.Link);
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Song first = (Song)lvSongs.Items[0];
                cefSong.Load(first.Link);
                playState = true;
                btnPlayerCtl.Content = "Pause";
                indexSong = 0;
            }
            catch (Exception)
            {
                MessageBox.Show("No songs in the playlist!");
                playState = false;
            }
        }

        private void btnPlayerCtl_Click(object sender, RoutedEventArgs e)
        {
            if (playState)
            {
                // Pause
                cefSong.Load("javascript:var mv = document.getElementById('movie_player'); mv.pauseVideo();");
                btnPlayerCtl.Content = "Play";
            }
            else
            {
                // Play
                cefSong.Load("javascript:var mv = document.getElementById('movie_player'); mv.playVideo();");
                btnPlayerCtl.Content = "Pause";
            }
            playState = !playState;
        }

        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (indexSong == 0)
            {
                indexSong = colSongs.Count - 1;
            }
            else
            {
                indexSong--;
            }
            Song play = colSongs[indexSong];
            cefSong.Load(play.Link);
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            tvServerBrowser.Items.Clear();
            int serverCounter = 0, channelCounter = 0, userCounter = 0;
            foreach (Server s in discord.Servers)
            {
                serverCounter++;
                TreeViewItem server = new TreeViewItem();
                server.Header = s.Name;
                TreeViewItem voice = new TreeViewItem(), text = new TreeViewItem();
                voice.Header = "Voice Channels";
                text.Header = "Text Channels";
                foreach (Discord.Channel c in s.VoiceChannels)
                {
                    channelCounter++;
                    TreeViewItem channel = new TreeViewItem();
                    channel.Header = c.Name;
                    foreach (Discord.User u in c.Users)
                    {
                        userCounter++;
                        channel.Items.Add(u.Name);
                    }
                    voice.Items.Add(channel);
                }
                foreach (Discord.Channel c in s.TextChannels)
                {
                    channelCounter++;
                    text.Items.Add(c);
                }
                server.Items.Add(voice);
                server.Items.Add(text);
                tvServerBrowser.Items.Add(server);
            }
            MessageBox.Show(string.Format("{0} servers, {1} channels and {2} users added!", serverCounter, channelCounter, userCounter));
        }

        private void btnStreamerConnect_Click(object sender, RoutedEventArgs e)
        {
            if (btnBotConnect.Content.ToString() != "Connect")
            {
                if (btnStreamerConnect.Content.ToString() == "Connect")
                {
                    ConnectStreamer();
                }
                else
                {
                    DisconnectStreamer();
                }
            }
            else
            {
                MessageBox.Show("You need to connect the bot account first before you can connect the streamer account.\n" +
                    "If you only want to use the bot functionality and not the Twitch dashboard login with your account as bot.",
                    "Streamer Chat Connect", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (client.GetMyUser().Partnered)
            {
                int streamDelay;

                try
                {
                    streamDelay = Convert.ToInt32(tbStreamDelay.Text);
                    if (streamDelay < 0 || streamDelay > 900)
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                }
                catch (Exception)
                {
                    streamDelay = 0;
                    MessageBox.Show("Invalid delay given of 0 up to and including 900.\nDelay is will be reverted to 0 seconds.",
                        "OakBot Channel Update", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                //client.Update(txtTitle.Text, cbGame.Text, Convert.ToString(streamDelay));
                client.Update(txtTitle.Text, cbGame.Text, streamDelay);
            }
            else
            {
                client.Update(txtTitle.Text, cbGame.Text);
            }

            MessageBox.Show("Channel information updated!",
                "Oakbot Channel Update", MessageBoxButton.OK, MessageBoxImage.Asterisk);
        }

        private void btnViewerAddPoints_Click(object sender, RoutedEventArgs e)
        {
            foreach (Viewer viewer in colViewers)
            {
                viewer.Points += 10;
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Utils.StartWebserver();
        }

        private void buttonBotConnect_Click(object sender, RoutedEventArgs e)
        {
            if (textBoxBotName.Text == "notSet" || string.IsNullOrWhiteSpace(textBoxBotName.Text))
            {
                MessageBox.Show("Please enter the bot Twitch username first before trying to connect with Twitch.",
                    "Bot Twitch Connect", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                WindowAuthBrowser tab = new WindowAuthBrowser(false);
                tab.Show();
            }
        }

        private void buttonStreamerConnect_Click(object sender, RoutedEventArgs e)
        {
            if (textBoxStreamerName.Text == "notSet" || string.IsNullOrWhiteSpace(textBoxStreamerName.Text))
            {
                MessageBox.Show("Please enter the streamer Twitch username first before trying to connect with Twitch if you want to use features that require a connected streamer account.",
                    "Streamer Twitch Connect", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                WindowAuthBrowser tab = new WindowAuthBrowser(true);
                tab.Show();
            }
        }

        private void cbAutoConnectBot_Checked(object sender, RoutedEventArgs e)
        {
            if (cbAutoConnectBot.IsChecked == true)
            {
                Config.AutoConnectBot = true;
            }
            else
            {
                Config.AutoConnectBot = false;
            }
        }

        private void cbAutoConnectBot_Unchecked(object sender, RoutedEventArgs e)
        {
            if (cbAutoConnectBot.IsChecked == false)
            {
                Config.AutoConnectBot = false;
            }
            else
            {
                Config.AutoConnectBot = true;
            }
        }

        private void cbAutoConnectStreamer_Checked(object sender, RoutedEventArgs e)
        {
            if (cbAutoConnectStreamer.IsChecked == true)
            {
                Config.AutoConnectStreamer = true;
            }
            else
            {
                Config.AutoConnectStreamer = false;
            }
        }

        private void cbAutoConnectStreamer_Unchecked(object sender, RoutedEventArgs e)
        {
            if (cbAutoConnectStreamer.IsChecked == false)
            {
                Config.AutoConnectStreamer = false;
            }
            else
            {
                Config.AutoConnectStreamer = true;
            }
        }

        private void ChatSend_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                // Get the line and
                if (!string.IsNullOrWhiteSpace(ChatSend.Text))
                {
                    // Speak as Streamer or Bot
                    if (SpeakAs.SelectedIndex == 0) // streamer
                    {
                        if (ChatSend.Text.StartsWith("/w"))
                        {
                            //streamerWhisperConnection.SendWhisper(ChatSend.Text);
                        }
                        else
                        {
                            // No need to append this message to the colChat,
                            // as the bot (primary) account will receive this message.
                            streamerChatConnection.SendChatMessage(ChatSend.Text);
                        }
                    }
                    else if (SpeakAs.SelectedIndex == 1) // Bot
                    {
                        if (ChatSend.Text.StartsWith("/w"))
                        {
                            //botWhisperConnection.SendWhisper(ChatSend.Text);
                        }
                        else
                        {
                            // Append this message to colChat in order
                            // to let the streamer see their own messages send.
                            colChatMessages.Add(new IrcMessage(accountBot.UserName, ChatSend.Text));
                            botChatConnection.SendChatMessage(ChatSend.Text);
                        }
                    }
                }

                // Clear the chat input
                ChatSend.Clear();
            }
        }

        private void colChatMessages_Changed(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                foreach (IrcMessage addedMessage in e.NewItems)
                {
                    var chatWindow = colChatWindows.FirstOrDefault(x => x.Viewer.UserName == addedMessage.Author);
                    if (chatWindow != null)
                    {
                        chatWindow.AddViewerMessage(addedMessage);
                    }
                }
            }
        }

        private void comboBox_Copy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // TODO
        }

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // TODO
        }

        private void ConnectBot()
        {
            try
            {
                botChat.Abort();
            }
            catch (ThreadAbortException)
            {
            }
            catch (Exception)
            {
            }

            // Disable UI elements
            textBoxBotName.IsEnabled = false;
            buttonBotConnect.IsEnabled = false;
            tbChannelName.IsEnabled = false;
            cbServerIP.IsEnabled = false;
            cbServerPort.IsEnabled = false;
            cbAutoConnectBot.IsEnabled = false;
            btnBotConnect.Content = "Disconnect";

            // Twitch Credentials
            accountBot = new TwitchCredentials(Config.BotUsername, Config.BotOAuthKey);

            // Start Bot connection and login
            botChatConnection = new TwitchChatConnection(accountBot);
            botChatConnection.JoinChannel(Config.ChannelName);

            // Create threads for the chat connections
            botChat = new Thread(new ThreadStart(botChatConnection.Run)) { IsBackground = true };

            // Start the chat connection threads
            botChat.Start();
        }

        private void ConnectStreamer()
        {
            try
            {
                streamerChat.Abort();
            }
            catch (ThreadAbortException)
            {
            }
            catch (Exception)
            {
            }

            // Twitch Credentials
            accountStreamer = new TwitchCredentials(Config.StreamerUsername, Config.StreamerOAuthKey);

            // Start Streamer connection and login
            streamerChatConnection = new TwitchChatConnection(accountStreamer, false);
            streamerChatConnection.JoinChannel(Config.ChannelName);

            // Create threads for the chat connections
            streamerChat = new Thread(new ThreadStart(streamerChatConnection.Run)) { IsBackground = true };

            // Start the chat connection threads
            streamerChat.Start();

            // TODO check on success login/connection
            if (true)
            {
                // Disable Settings UI elements
                textBoxStreamerName.IsEnabled = false;
                buttonStreamerConnect.IsEnabled = false;
                cbAutoConnectStreamer.IsEnabled = false;
                btnStreamerConnect.Content = "Disconnect";

                // Enable Twitch Dashboard tab
                tabMainDashboard.IsEnabled = true;

                try
                {
                    client = new TwitchAuthenticatedClient(Config.StreamerOAuthKey, Config.TwitchClientID);
                    txtTitle.Text = Utils.GetClient().GetMyChannel().Status;
                    cbGame.Text = Utils.GetClient().GetMyChannel().Game;
                    tbStreamDelay.Text = Utils.GetClient().GetMyChannel().Delay.ToString();

                    // Get Streamers Avatar
                    using (WebClient wc = new WebClient())
                    {
                        BitmapImage logo = new BitmapImage();
                        logo.BeginInit();
                        logo.StreamSource = wc.OpenRead(client.GetMyChannel().Logo);
                        logo.CacheOption = BitmapCacheOption.OnLoad;
                        logo.EndInit();
                        imgLogo.Source = logo;
                    }

                    // Enable partnered elements when partnered
                    if (client.GetMyUser().Partnered)
                    {
                        // Stream delay
                        tbStreamDelay.IsEnabled = true;

                        // Manual Commercials
                        gbManualCommercials.IsEnabled = true;
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }
            }
        }

        private bool DatabaseFilter(object item)
        {
            Viewer viewer = item as Viewer;
            return viewer.UserName.Contains(tbFilterOnName.Text);
        }

        private void Discord_MessageReceived(object sender, MessageEventArgs e)
        {
            string firstWord = Regex.Match(e.Message.Text, @"^\S+\b").Value.ToLower();
            UserCommand foundCommand = MainWindow.colBotCommands.FirstOrDefault(x =>
                x.Command == firstWord);

            if (foundCommand != null)
            {
                //foundCommand.ExecuteCommand(new IrcMessage(e.User.Name + "@Discord", e.Message.Text));
                foundCommand.ExecuteCommandDiscord(e.Message);
            }
            else
            {
                //BotCommands.RunBotCommand(firstWord, new IrcMessage(e.User.Name + "@Discord", e.Message.Text));
                BotCommands.RunBotCommandDiscord(firstWord, e.Message);
            }
            if (e.Channel.IsPrivate)
            {
                if(e.Message.Text.Split(' ')[0] == "confirm" && e.Message.Text.Split(' ').Length == 2)
                {
                    Discord.User confirm = e.User;
                    Models.Channel target = client.GetChannel(e.Message.Text.Split(' ')[1]);
                    string id = target.Game;
                    if(id == confirm.Id.ToString())
                    {
                        Viewer vwr = colDatabase.FirstOrDefault(x => x.UserName.ToLower() == target.Name.ToLower());
                        vwr.DiscordID = confirm.Id.ToString();
                        DatabaseUtils.UpdateViewer(vwr);
                    }
                }
            }
        }

        public static void popMsgBox(string text)
        {
            MessageBox.Show(text);
        }

        private void listViewChat_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (listViewChat.SelectedIndex != -1)
            {
                // Get the selected IrcMessage object
                IrcMessage selectedMessage = (IrcMessage)listViewChat.SelectedItem;

                // Get viewer's Viewer object to attach to the new chat window
                // This also prevents chat opening of "OakBot" system messages
                // Creating new Viewer objects is handled by TwitchChatConnection on time
                var isInDatabase = colDatabase.FirstOrDefault(x => x.UserName == selectedMessage.Author);
                if (isInDatabase != null)
                {
                    // Check if the child chat window is open already
                    var isChatOpen = colChatWindows.FirstOrDefault(x => x.Viewer.UserName == isInDatabase.UserName);
                    if (isChatOpen != null)
                    {
                        isChatOpen.Activate();
                    }
                    else
                    {
                        WindowViewerChat userChat = new WindowViewerChat(this, isInDatabase);
                        colChatWindows.Add(userChat);
                        userChat.Show();
                    }
                }
            }
        }

        private void lvSongs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lvSongs.SelectedIndex != -1)
            {
                Song selectedSong = (Song)lvSongs.SelectedItem;
                cefSong.Load(selectedSong.Link);
                playState = true;
                indexSong = lvSongs.SelectedIndex;
            }
        }

        private void lvViewerDatabase_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lvViewerDatabase.SelectedIndex != -1)
            {
                // Get the selected Viewer object
                Viewer selectedViewer = (Viewer)lvViewerDatabase.SelectedItem;

                // Check if the child chat window is open already
                // No need for validation as the Viewer is opened directly from the database collection
                var isChatOpen = colChatWindows.FirstOrDefault(x => x.Viewer.UserName == selectedViewer.UserName);
                if (isChatOpen != null)
                {
                    isChatOpen.Activate();
                }
                else
                {
                    WindowViewerChat userChat = new WindowViewerChat(this, selectedViewer);
                    colChatWindows.Add(userChat);
                    userChat.Show();
                }
            }
        }

        private void OakBot_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }

        private async void RunManualCommercial(int length)
        {
            int activationDelay;

            // Get given delay (0 up to and including 60) from textbox.
            // On error, default to 20 and show warning of the actual start.
            try
            {
                activationDelay = Convert.ToInt32(tbManComDelay.Text);
                if (activationDelay < 0 || activationDelay > 60)
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception)
            {
                activationDelay = 20;
                MessageBox.Show("Invalid delay given of 0 up to and including 60.\nA commercial will be requested in 20 seconds after pressing OK.",
                    "OakBot Manual Commercial", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // Create a new task with a sleep of the given delay
            // Await the task to retrieve exceptions
            try
            {
                Task activateCommercial = new Task(() =>
                {
                    Thread.Sleep(activationDelay * 1000);
                    TwitchResponse r = Utils.GetClient().TriggerCommercial(length);
                    if (r.Status == 422)
                    {
                        MessageBox.Show("Failed to start the commercial:\n" + r.Message,
                            "OakBot Manual Commercial", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show(string.Format("The {0} second commercial has started.", length),
                            "OakBot Manual Commercial", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    }
                });

                activateCommercial.Start();
                await activateCommercial;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong starting the commercial:\n" + ex.ToString(),
                    "OakBot Manual Commercial", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            cefSong.Load("javascript:var mv = document.getElementById('movie_player'); mv.setVolume(" + e.NewValue + "); ");
        }

        private void SpeakAs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (accountStreamer != null)
            {
                if (SpeakAs.SelectedIndex == 0) // streamer
                {
                    colChatMessages.Add(new IrcMessage("OakBot", string.Format("Speaking as {0}.", accountStreamer.UserName)));
                }
                else if (SpeakAs.SelectedIndex == 1) // bot
                {
                    colChatMessages.Add(new IrcMessage("OakBot", string.Format("Speaking as {0}.", accountBot.UserName)));
                }
            }
        }

        private void tbFilterOnName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (databaseView != null)
            {
                databaseView.Refresh();
                lblFilterCnt.Content = databaseView.Cast<Viewer>().Count();
            }
        }

        private void textBoxBotName_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBoxBotName.Text))
            {
                MessageBox.Show("Please enter the bot Twitch username.",
                    "Bot Twitch Username", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                Config.BotUsername = textBoxBotName.Text.Trim().ToLower();
            }
        }

        private void lvGiveaways_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Giveaway selected = colGiveaways.FirstOrDefault(x => x.GiveawayName == ((Giveaway)lvGiveaways.SelectedItem).GiveawayName);
            tbGwName.Text = selected.GiveawayName;
            tbTimeToEnter.Text = selected.GiveawayTime.TotalSeconds.ToString();
            tbKeyword.Text = selected.Keyword;
            tbPrice.Text = selected.Price.ToString();
            cbNeedsFollow.IsChecked = selected.NeedsFollow;
            slSubLuck.Value = selected.SubscriberLuck;
            tbResponseTime.Text = selected.ResponseTime.TotalSeconds.ToString();
            if(selected.Winner != null)
            {
                btnDrawWinner.IsEnabled = true;
            }else
            {
                btnDrawWinner.IsEnabled = false;
            }
        }

        private void btnAddEditGw_Click(object sender, RoutedEventArgs e)
        {
            Giveaway search = colGiveaways.FirstOrDefault(x => x.GiveawayName == tbGwName.Text);
            if(search == null)
            {
                search = new Giveaway(tbGwName.Text, new TimeSpan(0, 0, int.Parse(tbTimeToEnter.Text)), tbKeyword.Text, int.Parse(tbPrice.Text), cbNeedsFollow.IsChecked.Value, byte.Parse(slSubLuck.Value.ToString()), new TimeSpan(0, 0, int.Parse(tbResponseTime.Text)));
                search.ViewerEntered += DefaultGiveawayViewerEntered;
                search.WinnerChosen += DefaultGiveawayWinnerChosen;
                colGiveaways.Add(search);
            }else
            {
                search.GiveawayName = tbGwName.Text;
                search.GiveawayTime = new TimeSpan(0, 0, int.Parse(tbTimeToEnter.Text));
                search.Keyword = tbKeyword.Text;
                search.Price = int.Parse(tbPrice.Text);
                search.NeedsFollow = cbNeedsFollow.IsChecked.Value;
                search.SubscriberLuck = byte.Parse(slSubLuck.Value.ToString());
                search.ResponseTime = new TimeSpan(0, 0, int.Parse(tbResponseTime.Text));
            }
        }

        private void DefaultGiveawayViewerEntered(object sender, ViewerEnteredEventArgs e)
        {
            BotCommands.SendAndShowMessage(string.Format("{0} has entered the {1} giveaway!", e.Viewer, e.Giveaway.GiveawayName));
        }

        private void DefaultGiveawayWinnerChosen(object sender, WinnerChosenEventArgs e)
        {
            BotCommands.SendAndShowMessage(string.Format("{0} has won the {1} giveaway!", e.Winner.UserName, e.Giveaway.GiveawayName));
            btnDrawWinner.IsEnabled = true;
            WindowViewerChat vc = new WindowViewerChat(this, e.Winner);
        }

        private void txtToken_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBoxBotName.Text))
            {
                
            }
            else
            {
                Config.DiscordBotToken = txtToken.Text;
            }
        }

        private void btnStartGw_Click(object sender, RoutedEventArgs e)
        {
            Giveaway selected = colGiveaways.FirstOrDefault(x => x.GiveawayName == ((Giveaway)lvGiveaways.SelectedItem).GiveawayName);
            selected.Start();
        }

        private void btnStopGw_Click(object sender, RoutedEventArgs e)
        {
            Giveaway selected = colGiveaways.FirstOrDefault(x => x.GiveawayName == ((Giveaway)lvGiveaways.SelectedItem).GiveawayName);
            selected.Stop();
            btnDrawWinner.IsEnabled = true;
        }

        private void btnDrawWinner_Click(object sender, RoutedEventArgs e)
        {
            Giveaway selected = colGiveaways.FirstOrDefault(x => x.GiveawayName == ((Giveaway)lvGiveaways.SelectedItem).GiveawayName);
            selected.DrawWinner();
        }

        private void lvGiveaways_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(lvGiveaways.SelectedIndex != -1)
            {
                Giveaway selected = colGiveaways.First(x => x.GiveawayName == ((Giveaway)lvGiveaways.SelectedItem).GiveawayName);
                WindowGiveawayViewers gw = new WindowGiveawayViewers(selected);
                gw.Show();
            }
        }

        private void textBoxStreamerName_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBoxStreamerName.Text))
            {
                MessageBox.Show("Please enter the streamer Twitch username if you want to use the features that require a connected streamer account.",
                    "Streamer Twitch Username", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                Config.StreamerUsername = textBoxStreamerName.Text.Trim().ToLower();
            }
        }

        #endregion Private Methods

    }
}