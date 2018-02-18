using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace RadioIOT
{
    internal struct RadioChangeRequestEventArgs
    {
        public string Uri;
    }

    internal enum BotCommand
    {
        Start,
        Stop,
        Pause,
        VolumeUp,
        VolumeDown,
    }

    internal struct CommandRequestEventArgs
    {
        public BotCommand Command { get; set; }
    }

    delegate void RadioChangeRequestEventHandler(object sender, RadioChangeRequestEventArgs e);
    delegate void CommandRequestEventHandler(object sender, CommandRequestEventArgs e);

    internal class Bot
    {
        private readonly string _key;
        private readonly IDictionary<string, string> _stations;
        private long _chatId;
        private TelegramBotClient _bot;

        public Bot(string key, IDictionary<string, string> stations)
        {
            _key = key;
            _stations = stations;
        }

        public event RadioChangeRequestEventHandler RadioChangeRequest;
        public event CommandRequestEventHandler CommandRequest;

        public async Task SendMessage(string message)
        {
            await _bot.SendTextMessageAsync(
                    _chatId,
                    message);
        }

        public Task Start()
        {
            return Task.Run(async () =>
            {
                _bot = new TelegramBotClient(_key);

                var offset = 0;

                
                var buttons = new List<List<InlineKeyboardButton>>();
                var middleArray = new List<InlineKeyboardButton>();
                var i = 0;
                foreach (var key in _stations.Keys)
                {
                    if ( i % 3 == 0)
                    {
                        middleArray = new List<InlineKeyboardButton>();
                        buttons.Add(middleArray);
                    }
                    middleArray.Add(InlineKeyboardButton.WithCallbackData(key));
                    i++;
                }
                var stationButtons = new InlineKeyboardMarkup(buttons);

                while (true)
                {
                    var updates = await _bot.GetUpdatesAsync(offset);

                    foreach (var update in updates)
                    {
                        switch (update.Type)
                        {
                            case UpdateType.CallbackQuery:
                                var link = _stations[update.CallbackQuery.Data];
                                if (Uri.TryCreate(link, UriKind.Absolute, out Uri result)) { 
                                    RadioChangeRequest(this, new RadioChangeRequestEventArgs() { Uri = link });
                                    await _bot.AnswerCallbackQueryAsync(update.CallbackQuery.Id, link);
                                }
                                break;
                            case UpdateType.Message:
                                var message = update.Message;

                                if (message.From.Username != "eapyl") return;

                                _chatId = message.Chat.Id;

                                switch (message.Type)
                                {
                                    case MessageType.Text:

                                        if (message.Text == "/up")
                                            CommandRequest(this, new CommandRequestEventArgs { Command = BotCommand.VolumeUp });
                                        else if (message.Text == "/down")
                                            CommandRequest(this, new CommandRequestEventArgs { Command = BotCommand.VolumeDown });
                                        else if (message.Text == "/start")
                                            CommandRequest(this, new CommandRequestEventArgs { Command = BotCommand.Start });
                                        else if (message.Text == "/stop")
                                            CommandRequest(this, new CommandRequestEventArgs { Command = BotCommand.Stop });
                                        else if (message.Text == "/pause")
                                            CommandRequest(this, new CommandRequestEventArgs { Command = BotCommand.Pause });
                                        if (message.Text.StartsWith("/stations"))
                                        {
                                            await _bot.SendTextMessageAsync(
                                                message.Chat.Id,
                                                "Stations:",
                                                replyMarkup: stationButtons);
                                        }
                                        else if (Uri.TryCreate(message.Text, UriKind.Absolute, out Uri r))
                                        {
                                            RadioChangeRequest(this, new RadioChangeRequestEventArgs() { Uri = message.Text });
                                        }

                                        break;
                                }
                                break;
                        }

                        offset = update.Id + 1;
                    }
                }
            });
        }
    }
}
