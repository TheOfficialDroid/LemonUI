﻿#if SHVDN3
using GTA;
using System;
using GTA.Native;

namespace LemonUI.Phone
{
    /// <summary>
    /// A phone contact.
    /// </summary>
    public class PhoneContact
    {
        #region Fields

        private static readonly Sound busySound = new Sound("Phone_SoundSet_Default", "Remote_Engaged");
        private static readonly Sound callingSound = new Sound("Phone_SoundSet_Default", "Dial_and_Remote_Ring");
        private static readonly Sound hideSound = new Sound("Phone_SoundSet_Michael", "Put_Away");

        #endregion

        #region Events

        /// <summary>
        /// Event triggered when the contact is called by the player.
        /// </summary>
        public event CalledEventHandler Called;
        /// <summary>
        /// Event triggered when the call is answered by the player.
        /// </summary>
        /// <remarks>
        /// This event will trigger both, when the player calls the contact and when the contact calls the player.
        /// </remarks>
        public event ConnectedEventHandler Connected;
        /// <summary>
        /// Event triggered when the call is hung up by the player.
        /// </summary>
        public event EventHandler Cancelled;
        /// <summary>
        /// Event triggered when the call finishes naturally, without player input.
        /// </summary>
        public event EventHandler Finished;

        #endregion

        #region Properties

        /// <summary>
        /// The name of the contact.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The icon of the contact.
        /// </summary>
        public string Icon { get; set; } = "CHAR_DEFAULT";

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new contact.
        /// </summary>
        /// <param name="name">The name of the contact.</param>
        public PhoneContact(string name)
        {
            Name = name;
        }

        #endregion

        #region Tools

        private static void RestoreScript(string script, int stack)
        {
            if (Function.Call<bool>(Hash._GET_NUMBER_OF_REFERENCES_OF_SCRIPT_WITH_NAME_HASH, Game.GenerateHash("appcontacts") > 0))
            {
                return;
            }

            while (!Function.Call<bool>(Hash.HAS_SCRIPT_LOADED, script))
            {
                Function.Call(Hash.REQUEST_SCRIPT, script);
            }

            Function.Call(Hash.START_NEW_SCRIPT, script, stack);
        }

        #endregion

        #region Functions

        internal void Call(Scaleform.Phone phone)
        {
            const string dialing = "CELL_211";
            const string busy = "CELL_220";
            const string connected = "CELL_219";

            phone.SetButtonIcon(1, 1);
            phone.SetButtonIcon(2, 1);
            phone.SetButtonIcon(3, 6);

            callingSound.PlayFrontend(false);
            phone.ShowCalling(Name, dialing, Icon);

            Game.Player.Character.Task.UseMobilePhone();
            CalledEventArgs called = new CalledEventArgs(this);
            Called?.Invoke(phone, called);

            int waitUntil = Game.GameTime + called.Wait;

            while (waitUntil > Game.GameTime)
            {
                phone.ShowCalling(Name, dialing, Icon);

                if (Game.IsControlJustPressed(Control.PhoneCancel))
                {
                    callingSound.Stop();
                    return;
                }

                Script.Yield();
            }

            callingSound.Stop();

            switch (called.Behavior)
            {
                case CallBehavior.Busy:
                    busySound.PlayFrontend(false);

                    while (!Game.IsControlPressed(Control.PhoneCancel))
                    {
                        phone.ShowCalling(Name, busy, Icon);
                        Script.Yield();
                    }

                    busySound.Stop();
                    Game.Player.Character.Task.PutAwayMobilePhone();
                    phone.ShowPage(2);
                    RestoreScript("appContacts", 4000);
                    break;
                case CallBehavior.Available:
                    phone.ShowCalling(Name, connected, Icon);
                    Connected?.Invoke(phone, new ConnectedEventArgs(this));
                    break;
            }
        }

        #endregion
    }
}
#endif
