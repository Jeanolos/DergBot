using System;

namespace OakBot.Args
{
    public class WinnerChosenEventArgs : EventArgs
    {
        #region Private Fields

        private Viewer winner;

        private Giveaway giveaway;

        #endregion Private Fields

        #region Public Constructors

        public WinnerChosenEventArgs(Viewer viewer, Giveaway giveaway)
        {
            winner = viewer;
            this.giveaway = giveaway;
        }

        #endregion Public Constructors

        #region Public Properties

        public Viewer Winner
        {
            get
            {
                return winner;
            }
        }

        public Giveaway Giveaway
        {
            get
            {
                return giveaway;
            }set
            {
                giveaway = value;
            }
        }

        #endregion Public Properties
    }
}