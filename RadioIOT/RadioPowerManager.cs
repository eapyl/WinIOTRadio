namespace RadioIOT
{
    internal enum PowerState
    {
        Standby,
        Powered
    }

    internal struct PowerStateChangedEventArgs
    {
        public PowerState PowerState;
    }

    delegate void PowerStateChangedEventHandler(object sender, PowerStateChangedEventArgs e);

    interface IDevicePowerManager
    {
        event PowerStateChangedEventHandler PowerStateChanged;

        PowerState PowerState
        {
            get;
            set;
        }

        bool CanPerformActions();
    }

    class RadioPowerManager : IDevicePowerManager
    {
        private PowerState _powerState;
        public PowerState PowerState
        {
            get
            {
                return this._powerState;
            }

            set
            {
                if (value != this._powerState)
                {
                    _powerState = value;
                    PowerStateChanged(this, new PowerStateChangedEventArgs() { PowerState = this._powerState });
                }
            }
        }

        public event PowerStateChangedEventHandler PowerStateChanged;

        public bool CanPerformActions() => this.PowerState == PowerState.Powered;
    }
}
