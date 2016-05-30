using System;

namespace OakBot.Args
{
    public class ViewerEnteredEventArgs : EventArgs
    {
        #region Private Fields

        private string viewer;

        private Giveaway giveaway;

        #endregion Private Fields

        #region Public Constructors

        public ViewerEnteredEventArgs(string viewer, Giveaway gw)
        {
            this.viewer = viewer;
            giveaway = gw;
        }

        #endregion Public Constructors

        #region Public Properties

        public string Viewer
        {
            get
            {
                return viewer;
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