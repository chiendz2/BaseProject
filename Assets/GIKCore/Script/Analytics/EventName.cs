namespace GIKCore
{
    public static class EventName
    {
        public const string FirstOpen = "first_open";
        public const string SessionStart = "session_start";

        public const string LevelStart = "level_start";
        public const string LevelWin = "level_win";
        public const string LevelFail = "level_fail";
        public const string LevelRetry = "level_retry";
        public const string LevelQuit = "level_quit";
        public const string LevelSkip = "level_skip";

        public const string TutorialBegin = "tutorial_begin";
        public const string TutorialStep = "tutorial_step";
        public const string TutorialComplete = "tutorial_complete";

        public const string ScreenView = "screen_view";
        public const string ButtonClick = "button_click";
        public const string PopupOpen = "popup_open";
        public const string PopupClose = "popup_close";

        public const string AdRequest = "ad_request";
        public const string AdLoaded = "ad_loaded";
        public const string AdLoadFail = "ad_load_fail";
        public const string AdShow = "ad_show";
        public const string AdClick = "ad_click";
        public const string AdReward = "ad_reward";
        public const string AdRevenue = "ad_revenue";

        public const string IapStart = "iap_start";
        public const string IapSuccess = "iap_success";
        public const string IapFail = "iap_fail";
        public const string IapRestore = "iap_restore";

        public const string ResourceEarn = "resource_earn";
        public const string ResourceSpend = "resource_spend";

        public const string AfSession = "af_session";
        public const string AfTutorialCompletion = "af_tutorial_completion";
        public const string AfInterLoaded = "af_inter_successfullyloaded";
        public const string AfInterDisplayed = "af_inter_displayed";
        public const string AfRewardedLoaded = "af_rewarded_successfullyloaded";
        public const string AfRewardedDisplayed = "af_rewarded_displayed";
        public const string AfPurchase = "af_purchase";
        public const string AfAdRevenue = "af_ad_revenue";
    }
}
