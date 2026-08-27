using System;

namespace GIKCore
{
    [Serializable]
    public class UserData
    {
        public const int CurrentVersion = 1;

        public int dataVersion;
        public int coin;
        public int currentLevel;
        public bool soundOn;
        public bool musicOn;
        public bool vibrationOn;
        public bool noAds;
        public int language;
        public string firstOpenUtc;
        public string lastOpenUtc;
        public string userPseudoId;

        public UserData()
        {
            dataVersion = CurrentVersion;
            coin = 0;
            currentLevel = 1;
            soundOn = true;
            musicOn = true;
            vibrationOn = true;
            noAds = false;
            language = -1;
            firstOpenUtc = string.Empty;
            lastOpenUtc = string.Empty;
            userPseudoId = string.Empty;
        }
    }
}
