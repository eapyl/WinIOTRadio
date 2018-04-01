using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace RadioIOT
{
    public struct RadioChangeRequestEventArgs
    {
        public string Uri;
    }

    public enum BotCommand
    {
        Start,
        Stop,
        Pause,
        VolumeUp,
        VolumeDown,
    }

    public struct CommandRequestEventArgs
    {
        public BotCommand Command;
    }

    public delegate void RadioChangeRequestEventHandler(object sender, RadioChangeRequestEventArgs e);
    public  delegate void CommandRequestEventHandler(object sender, CommandRequestEventArgs e);

    public sealed class Bot
    {
        private readonly string _key;
        private readonly string _owner;
        private readonly string _link;
        private long _chatId;
        private TelegramBotClient _bot;

        public Bot(string key, string owner, string link)
        {
            _key = key;
            _owner = owner;
            _link = link;
        }

        public event RadioChangeRequestEventHandler RadioChangeRequest;
        public event CommandRequestEventHandler CommandRequest;

        public async void SendMessage(string message)
        {
            await _bot.SendTextMessageAsync(
                    _chatId,
                    message).AsAsyncAction();
        }

        public async void Start()
        {
            _bot = new TelegramBotClient(_key);

            var offset = 0;

            while (true)
            {
                foreach (var update in await _bot.GetUpdatesAsync(offset))
                {
                    switch (update.Type)
                    {
                        case UpdateType.CallbackQuery:
                            var link = update.CallbackQuery.Data;
                            if (Uri.TryCreate(link, UriKind.Absolute, out Uri result)) { 
                                RadioChangeRequest(this, new RadioChangeRequestEventArgs() { Uri = link });
                                await _bot.AnswerCallbackQueryAsync(update.CallbackQuery.Id, link);
                            }
                            break;
                        case UpdateType.Message:
                            var message = update.Message;

                            if (message.From.Username != _owner) return;

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
                                    else if (message.Text.StartsWith("/stations"))
                                    {
                                        var buttons = new List<List<InlineKeyboardButton>>();
                                        var middleArray = new List<InlineKeyboardButton>();
                                        var i = 0;
                                        var stations = await Download();
                                        foreach (var key in stations.Keys)
                                        {
                                            if (i % 3 == 0)
                                            {
                                                middleArray = new List<InlineKeyboardButton>();
                                                buttons.Add(middleArray);
                                            }
                                            middleArray.Add(InlineKeyboardButton.WithCallbackData(key, stations[key]));
                                            i++;
                                        }
                                        var stationButtons = new InlineKeyboardMarkup(buttons);

                                        await _bot.SendTextMessageAsync(
                                            message.Chat.Id,
                                            "Stations:",
                                            replyMarkup: stationButtons);
                                    }
                                    else if (Uri.TryCreate(message.Text, UriKind.Absolute, out Uri r))
                                    {
                                        RadioChangeRequest(this, new RadioChangeRequestEventArgs() { Uri = message.Text });
                                    }
                                    else
                                    {
                                        await _bot.SendTextMessageAsync(
                                            message.Chat.Id,
                                            "Can't udnerstand command");
                                    }

                                    break;
                            }
                            break;
                    }

                    offset = update.Id + 1;
                }
            }
        }

        private async Task<IDictionary<string, string>> Download()
        {
            var stations = new Dictionary<string, string>();
            var client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(new Uri(_link));
            var str = await response.Content.ReadAsStringAsync();

            var values = str.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            for (uint i = 0; i < values.Length; i++)
            {
                var v = values[i].Split('=', StringSplitOptions.RemoveEmptyEntries);
                if (v.Length != 2) continue;
                stations.Add(v[0], v[1]);
            }

            return stations;
        }
    }
}
