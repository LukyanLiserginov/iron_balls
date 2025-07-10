using System;
using System.Collections.Generic;
using System.Text;

namespace ironballs
{
    internal static class MySetup
    {
        private static int _players;
        private static int _gameMode = 1;
        internal static int Players
        {
            get { return _players; }
            set 
            {
                if (value < 0) _players = 0;
                else if (value > 4) _players = 4;
                else _players = value;
            }
        }
        internal static int GameMode
        {
            get { return _gameMode; }
            set
            {
                if (value < 1) _gameMode = 1;
                else if (value > 3) _gameMode = 3;
                else _gameMode = value;
            }
        }
    }
}
