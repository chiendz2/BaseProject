#if DOTWEEN_ENABLED
using System;
using System.Linq;
using DG.Tweening;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    [Serializable]
    public sealed class DOTweenAnimationStep : GameObjectAnimationStep
    {
        public override string DisplayName => "Tween Target";
        [SerializeField]
        private int loopCount;
        public int LoopCount
        {
            get => loopCount;
            set => loopCount = value;
        }

        [SerializeField]
        private LoopType loopType;
        public LoopType LoopType
        {
            get => loopType;
            set => loopType = value;
        }

        [SerializeReference]
        private DOTweenActionBase[] actions;
        public DOTweenActionBase[] Actions
        {
            get => actions;
            set => actions = value;
        }

        public override void AddTweenToSequence(Sequence animationSequence)
        {
            // Target/actions bỏ trống -> bỏ qua step. Ném NRE ở đây sẽ giết luôn Awake của
            // AnimationSequencerController, kéo theo cả những step phía sau không được dựng.
            if (target == null || actions == null)
            {
                Debug.LogWarning("AnimationSequencer: DOTweenAnimationStep chưa gán Target - đã bỏ qua step này.");
                return;
            }

            Sequence sequence = DOTween.Sequence();
            for (int i = 0; i < actions.Length; i++)
            {
                if (actions[i] == null) continue;
                Tween tween = actions[i].GenerateTween(target, duration);
                if (i == 0)
                {
                    tween.SetDelay(Delay);
                }
                sequence.Join(tween);
            }

            sequence.SetLoops(loopCount, loopType);
            
            if (FlowType == FlowType.Join)
                animationSequence.Join(sequence);
            else
                animationSequence.Append(sequence);

        }

        public override void ResetToInitialState()
        {
            if (actions == null) return;

            for (int i = actions.Length - 1; i >= 0; i--)
            {
                if (actions[i] == null) continue;
                actions[i].ResetToInitialState();
            }
        }

        public override string GetDisplayNameForEditor(int index)
        {
            string targetName = "NULL";
            if (target != null)
                targetName = target.name;
            
            return $"{index}. {targetName}: {String.Join(", ", actions.Select(action => action.DisplayName)).Truncate(45)}";
        }

        public bool TryGetActionAtIndex<T>(int index, out T result) where T: DOTweenActionBase
        {
            if (index < 0 || index > actions.Length - 1)
            {
                result = null;
                return false;
            }

            result = actions[index] as T;
            return result != null;
        }
    }
}
#endif