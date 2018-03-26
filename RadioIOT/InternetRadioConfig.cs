using System.Threading.Tasks;
using System;
using Windows.Storage;
using System.Xml.Linq;
using System.Linq;
using Windows.Foundation;

namespace RadioIOT
{
    public sealed class InternetRadioConfig
    {
        public int Delay { get; private set; }

        public string Key { get; private set; }

        public string Owner { get; private set; }

        public static IAsyncOperation<InternetRadioConfig> GetDefault()
        {
            return Task.Run(async () =>
            {
                var config = new InternetRadioConfig();
                await config.LoadAsync();
                return config;
            }).AsAsyncOperation();
        }

        private async Task LoadAsync()
        {
            var packageFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var settingsFile = await packageFolder.GetFileAsync("settings.xml");

            var xmlString = await FileIO.ReadTextAsync(settingsFile);
            var xml = XElement.Parse(xmlString);

            Key = xml.Descendants("add").Single(x => x.Attribute(XName.Get("key")).Value == "key").Attribute(XName.Get("value")).Value;
            Delay = Convert.ToInt32(xml.Descendants("add").Single(x => x.Attribute(XName.Get("key")).Value == "delay").Attribute(XName.Get("value")).Value);
            Owner = xml.Descendants("add").Single(x => x.Attribute(XName.Get("key")).Value == "owner").Attribute(XName.Get("value")).Value;
        }
    }
}
