using System.Collections.Generic;
using GamePopup;
using GameReward;
using UnityEngine;
using UnityEngine.Purchasing;

namespace GameIAP
{
    public class GameIAPManager : IAPManager
    {
        // Hard-code tiếng Anh cho khớp các notice sẵn có trong project (xem AdsUtils).
        private const string PurchaseNoticeTitle = "Purchase Completed";
        private const string PurchaseNoticeContent = "Purchase successful. The rewards have been added to your account";
        private const string FullWorldTourPurchasePendingKeyPrefix = "worldtour_full_purchase_pending_";

        public static GameIAPManager Instance;

        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        protected virtual void Start()
        {
            GameEvents.OnRemoteConfigLoaded += OnRemoteConfigLoaded;

            if (RemoteConfig.Loaded) OnRemoteConfigLoaded();
        }

        private void OnDestroy()
        {
            GameEvents.OnRemoteConfigLoaded -= OnRemoteConfigLoaded;
        }

        private void OnRemoteConfigLoaded()
        {
            if (m_StoreController == null)
            {
                Initialize();
            }
        }

        protected override void AddProducts(ConfigurationBuilder builder)
        {
            base.AddProducts(builder);

            var config = RemoteConfig.Instance.ShopIAPConfig;
            var productIds = new HashSet<string>();

            if (config.Groups != null)
            {
                for (int i = 0; i < config.Groups.Length; i++)
                {
                    var packs = config.Groups[i]?.Packs;
                    if (packs == null) continue;

                    for (int j = 0; j < packs.Length; j++)
                    {
                        AddProduct(builder, packs[j], productIds);
                    }
                }
            }

            AddProduct(builder, config.PiggyBank, productIds);
            AddProduct(builder, config.LimitedPack, productIds);

            var worldTour = config.WorldTour;
            if (worldTour != null && !string.IsNullOrEmpty(worldTour.Id) && productIds.Add(worldTour.Id))
                builder.AddProduct(worldTour.Id, ProductType.Consumable);
        }

        public override void BuyProduct(string productId)
        {
            if (GamePrefs.IsIAPFree || Application.platform == RuntimePlatform.WindowsEditor)
            {
                InvokeOnItemPurchased(productId, true);
            }
            else
            {
                base.BuyProduct(productId);
            }
        }

        public void BuyWorldTourFromBanner(string productId)
        {
            if (string.IsNullOrEmpty(productId))
                return;

            // Store callback có thể về sau khi Shop/banner đã bị đóng hoặc app được mở lại.
            // Persist nguồn mua để manager vẫn cấp đủ toàn bộ World Tour mà không phụ thuộc UI.
            PlayerPrefs.SetInt(GetFullWorldTourPurchasePendingKey(productId), 1);
            PlayerPrefs.Save();
            BuyProduct(productId);
        }

        protected override void InvokeOnItemPurchased(string id, bool success)
        {
            base.InvokeOnItemPurchased(id, success);
            var isFullWorldTourPurchase = IsFullWorldTourPurchasePending(id);
            var granted = false;
            if (success)
            {
                granted = ReceiveRewards(id, isFullWorldTourPurchase);
                int buyCount = GetBuyCount(id);
                SetBuyCount(id, buyCount + 1);
                AnalyticManager.OnIapCount?.Invoke();
            }

            if (isFullWorldTourPurchase)
            {
                PlayerPrefs.DeleteKey(GetFullWorldTourPurchasePendingKey(id));
                PlayerPrefs.Save();
            }

            GameEvents.OnBuyProductIAPCompleted?.Invoke(id, success);

            // Chỉ báo sau khi quà ĐÃ vào tài khoản. Đặt sau OnBuyProductIAPCompleted để các
            // popup nghe event kịp dựng lại UI, và để PopupNotice giữ sorting order cao nhất
            // (PopupBase cấp order tăng dần) -> nó luôn nằm trên cùng.
            if (granted && ShouldNoticeOnPurchase(id, isFullWorldTourPurchase))
                ShowPurchaseSuccessNotice();
        }

        /// <summary>
        /// World Tour mua trong popup chỉ cấp Today và chờ Claim All nên không hiện notice.
        /// Riêng giao dịch từ banner được cấp đủ toàn bộ quà ngay và hiện notice như pack thường.
        /// </summary>
        private static bool ShouldNoticeOnPurchase(string id, bool isFullWorldTourPurchase)
        {
            if (isFullWorldTourPurchase)
                return true;

            var worldTour = RemoteConfig.Instance != null
                ? RemoteConfig.Instance.ShopIAPConfig?.WorldTour
                : null;
            return worldTour == null || id != worldTour.Id;
        }

        private static string GetFullWorldTourPurchasePendingKey(string id)
        {
            return FullWorldTourPurchasePendingKeyPrefix + id;
        }

        private static bool IsFullWorldTourPurchasePending(string id)
        {
            return !string.IsNullOrEmpty(id)
                   && PlayerPrefs.GetInt(GetFullWorldTourPurchasePendingKey(id), 0) == 1;
        }

        private static void ShowPurchaseSuccessNotice()
        {
            GameEvents.ShowPopup?.Invoke(PopupId.PopupNotice, go =>
            {
                var notice = go.GetComponent<PopupNotice>();
                if (notice != null)
                    notice.Show(PurchaseNoticeTitle, PurchaseNoticeContent);
            });
        }

        public int GetBuyCount(string id)
        {
            return PlayerPrefs.GetInt($"packbuycount_{id}", 0);
        }

        public void SetBuyCount(string id, int count)
        {
            PlayerPrefs.SetInt($"packbuycount_{id}", count);
        }

        /// <summary>Trả về true nếu thực sự có quà được cộng vào tài khoản.</summary>
        private bool ReceiveRewards(string id, bool grantFullWorldTour)
        {
            // Mua trong PopupWorldTourPack chỉ cấp Today; mua từ banner cấp đủ cả hai nhóm Free.
            var worldTour = RemoteConfig.Instance.ShopIAPConfig?.WorldTour;
            if (worldTour != null && id == worldTour.Id)
            {
                var granted = GrantRewards(worldTour.TodayRewards);
                if (grantFullWorldTour)
                {
                    var hasFreeRewards =
                        HasRewards(worldTour.Free1Rewards)
                        || HasRewards(worldTour.Free2Rewards);
                    PopupWorldTourPack.GrantFullPackRewards(worldTour);
                    granted |= hasFreeRewards;
                }

                return granted;
            }

            var pack = FindPack(id);
            if (pack?.Rewards == null)
            {
                // Config thiếu/sai id -> tiền đã trừ mà không có quà. Không được báo thành công.
                Debug.LogWarning($"[IAP] Reward pack not found for product '{id}'.");
                return false;
            }

            return GrantRewards(pack.Rewards);
        }

        private static bool HasRewards(Reward[] rewards)
        {
            return rewards != null && rewards.Length > 0;
        }

        private bool GrantRewards(Reward[] rewards)
        {
            if (rewards == null || rewards.Length == 0) return false;
            for (int i = 0; i < rewards.Length; i++)
            {
                GlobalValues.ClaimReward(rewards[i], Location.ShopIAP, ParameterValue.buy);
            }

            return true;
        }

        public ShopIAPPack FindPack(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            var config = RemoteConfig.Instance.ShopIAPConfig;
            if (config.LimitedPack != null && id == config.LimitedPack.Id) return config.LimitedPack;
            if (config.PiggyBank != null && id == config.PiggyBank.Id) return config.PiggyBank;

            if (config.Groups != null)
            {
                for (int i = 0; i < config.Groups.Length; i++)
                {
                    var packs = config.Groups[i]?.Packs;
                    if (packs == null) continue;

                    for (int j = 0; j < packs.Length; j++)
                    {
                        if (packs[j] != null && id == packs[j].Id) return packs[j];
                    }
                }
            }

            return null;
        }

        private static void AddProduct(
            ConfigurationBuilder builder,
            ShopIAPPack pack,
            HashSet<string> productIds)
        {
            if (pack == null || string.IsNullOrEmpty(pack.Id) || !productIds.Add(pack.Id)) return;
            builder.AddProduct(pack.Id, ProductType.Consumable);
        }
    }
}
