using UnityEngine;
using System;
using System.Collections;

#if UNITY_ANDROID
using Unity.Notifications.Android;
#elif UNITY_IOS
using Unity.Notifications.iOS;
#endif

public class NotificationManager : MonoBehaviour
{
    [Header("Android Channel Settings")]
    public string channelId = "chef_channel";
    public string channelName = "Chef Notifications";
    public string channelDescription = "Daily kitchen alerts";

    private bool IsCompleted
    {
        get => PlayerPrefs.GetInt("IsNotificationsCompleted", 0) == 1;
        set => PlayerPrefs.SetInt("IsNotificationsCompleted", value ? 1 : 0);
    }

    void Start()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (IsCompleted) return;
        IsCompleted = true;

#if UNITY_ANDROID
        if (IsAndroid13OrHigher())
        {
            StartCoroutine(RequestNotificationPermission());
        }
        else
        {
            RegisterNotificationChannel();
            ScheduleDailyNotifications();
        }
#elif UNITY_IOS
        ScheduleDailyNotifications();
#endif
#endif
    }

    void RegisterNotificationChannel()
    {
#if UNITY_ANDROID
        var channel = new AndroidNotificationChannel()
        {
            Id = channelId,
            Name = channelName,
            Importance = Importance.Default,
            Description = channelDescription
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);
#endif
    }

    void ScheduleDailyNotifications()
    {
        ScheduleNotification(1, "🔥 The grill's still warm, Chef! Come finish today's orders!", 11, 30);
        ScheduleNotification(2, "🍗 Hungry customers are back! Let's cook up something tasty!", 18, 0);
        ScheduleNotification(3, "🎁 Daily Reward unlocked! Don't let it cool off, Chef!", 11, 30);
        ScheduleNotification(4, "🍢 Perfect skewers don't grill themselves! Fire up the stove!", 18, 0);
        ScheduleNotification(5, "😋 Your grill misses you... and the flavor combo's waiting!", 11, 30);
        ScheduleNotification(6, "🧑‍🍳 You're on fire, Chef! Let's push for that next kitchen unlock!", 18, 0);
        ScheduleNotification(7, "🌟 Week complete! Claim your Daily bonus before it's gone!", 11, 30);
    }

    void ScheduleNotification(int dayOffset, string message, int hour, int minute)
    {
        DateTime fireTime = DateTime.Today.AddDays(dayOffset).AddHours(hour).AddMinutes(minute);

#if UNITY_ANDROID
        var notification = new AndroidNotification()
        {
            Text = message,
            FireTime = fireTime,
            ShowInForeground = false,
            SmallIcon = "icon_2",
            LargeIcon = "icon_1",
            Color = new Color(1f, .6f, 0f, 1f)
        };
        AndroidNotificationCenter.SendNotification(notification, channelId);
#endif

#if UNITY_IOS
        var iosNotification = new iOSNotification()
        {
            Identifier = $"chef_day_{dayOffset}",
            Body = message,
            ShowInForeground = false,
            Trigger = new iOSNotificationTimeIntervalTrigger()
            {
                TimeInterval = fireTime - DateTime.Now,
                Repeats = false
            }
        };
        iOSNotificationCenter.ScheduleNotification(iosNotification);
#endif
    }

#if UNITY_ANDROID
    private bool IsAndroid13OrHigher()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            string os = SystemInfo.operatingSystem;
            int apiIndex = os.IndexOf("API-");
            if (apiIndex != -1 && int.TryParse(os.Substring(apiIndex + 4), out int apiLevel))
            {
                return apiLevel >= 33;
            }
        }
        return false;
    }

    private IEnumerator RequestNotificationPermission()
    {
        var request = new PermissionRequest();
        while (request.Status == PermissionStatus.RequestPending)
            yield return null;

        if (request.Status == PermissionStatus.Allowed)
        {
            RegisterNotificationChannel();
            ScheduleDailyNotifications();
        }
    }
#endif
}
