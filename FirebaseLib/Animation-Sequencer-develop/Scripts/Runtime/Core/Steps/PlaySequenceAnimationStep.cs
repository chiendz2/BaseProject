#if DOTWEEN_ENABLED
using System;
using DG.Tweening;
using UnityEngine;

namespace BrunoMikoski.AnimationSequencer
{
    [Serializable]
    public sealed class PlaySequenceAnimationStep : AnimationStepBase
    {
        public override string DisplayName => "Play Sequence";

        [SerializeField]
        private AnimationSequencerController sequencer;
        public AnimationSequencerController Sequencer
        {
            get => sequencer;
            set => sequencer = value;
        }

        public override void AddTweenToSequence(Sequence animationSequence)
        {
            // Slot bỏ trống trong Inspector -> bỏ qua step thay vì ném NRE. Trước đây exception này
            // nổ ngay trong AnimationSequencerController.Awake() nên phần Awake còn lại không chạy,
            // và khi controller được Addressables instantiate thì nó còn cắt luôn chuỗi completion
            // callback của Addressables.
            if (sequencer == null)
            {
                Debug.LogWarning("AnimationSequencer: PlaySequenceAnimationStep chưa gán Sequencer - đã bỏ qua step này.");
                return;
            }

            Sequence sequence = sequencer.GenerateSequence();
            sequence.SetDelay(Delay);
            if (FlowType == FlowType.Join)
                animationSequence.Join(sequence);
            else
                animationSequence.Append(sequence);
        }

        public override void ResetToInitialState()
        {
            if (sequencer == null) return;

            sequencer.ResetToInitialState();
        }

        public override string GetDisplayNameForEditor(int index)
        {
            string display = "NULL";
            if (sequencer != null)
                display = sequencer.name;
            return $"{index}. Play {display} Sequence";
        }

        public void SetTarget(AnimationSequencerController newTarget)
        {
            sequencer = newTarget;
        }
    }
}
#endif