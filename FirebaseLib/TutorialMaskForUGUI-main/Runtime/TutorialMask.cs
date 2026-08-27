using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace CodeWriter.UIExtensions
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Graphic))]
    [AddComponentMenu("CodeWriter/UIExtensions/Tutorial Mask")]
    public class TutorialMask : UIBehaviour, IMaterialModifier, ICanvasRaycastFilter
    {
        [SerializeField]
        [Tooltip("Resets the mask, useful if you want to show multiple tutorials on top of each other")]
        private bool clearMask = true;

        [SerializeField]
        private bool invert;

        [SerializeField]
        private BlocksRaycasts blocksRaycasts = BlocksRaycasts.All;

        [NonSerialized]
        private Graphic _graphic;

        [NonSerialized]
        private Material _maskMaterial;

        [NonSerialized]
        private Material _unMaskMaterial;

        [NonSerialized]
        private int _tempIgnoreRaycasts;

        public Graphic Graphic => _graphic ?? (_graphic = GetComponent<Graphic>());

        public BlocksRaycasts BlockRaycasts
        {
            get => blocksRaycasts;
            set => blocksRaycasts = value;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (Graphic != null)
            {
                if (clearMask)
                {
                    Graphic.canvasRenderer.hasPopInstruction = true;
                }

                Graphic.SetMaterialDirty();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (Graphic != null)
            {
                Graphic.SetMaterialDirty();
                Graphic.canvasRenderer.hasPopInstruction = false;
                Graphic.canvasRenderer.popMaterialCount = 0;
            }

            StencilMaterial.Remove(_maskMaterial);
            _maskMaterial = null;

            StencilMaterial.Remove(_unMaskMaterial);
            _unMaskMaterial = null;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (!IsActive())
            {
                return;
            }

            if (Graphic != null)
            {
                Graphic.SetMaterialDirty();
            }
        }
#endif

        public Material GetModifiedMaterial(Material baseMaterial)
        {
            if (!IsActive())
            {
                return baseMaterial;
            }

            const int tutorialBit = 1 << 7;

            var maskMat = StencilMaterial.Add(baseMaterial,
                stencilID: tutorialBit,
                operation: StencilOp.Keep,
                compareFunction: invert ? CompareFunction.Equal : CompareFunction.NotEqual,
                colorWriteMask: ColorWriteMask.All,
                readMask: tutorialBit,
                writeMask: 0
            );
            StencilMaterial.Remove(_maskMaterial);
            _maskMaterial = maskMat;

            if (clearMask)
            {
                var unMaskMat = StencilMaterial.Add(baseMaterial,
                    stencilID: tutorialBit,
                    operation: StencilOp.Zero,
                    compareFunction: CompareFunction.Always,
                    colorWriteMask: 0,
                    readMask: 255,
                    writeMask: tutorialBit
                );
                StencilMaterial.Remove(_unMaskMaterial);
                _unMaskMaterial = unMaskMat;

                Graphic.canvasRenderer.hasPopInstruction = true;
                Graphic.canvasRenderer.popMaterialCount = 1;
                Graphic.canvasRenderer.SetPopMaterial(_unMaskMaterial, 0);
            }
            else
            {
                Graphic.canvasRenderer.hasPopInstruction = false;
                Graphic.canvasRenderer.popMaterialCount = 0;
            }

            return maskMat;
        }

        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            if (_tempIgnoreRaycasts > 0)
            {
                return false;
            }

            switch (blocksRaycasts)
            {
                case BlocksRaycasts.None:
                    return false;

                case BlocksRaycasts.All:
                    return true;

                case BlocksRaycasts.OutsideEnabledTutorialObjects:
                    try
                    {
                        _tempIgnoreRaycasts++;

                        using (ListPool<RaycastResult>.Get(out var hits))
                        {
                            var eventSystem = EventSystem.current;
                            var eventData = new PointerEventData(eventSystem)
                            {
                                position = sp,
                            };

                            eventSystem.RaycastAll(eventData, hits);

                            foreach (var hit in hits)
                            {
                                if (hit.gameObject.TryGetComponent(out TutorialObject tutorialObject) &&
                                    tutorialObject.isActiveAndEnabled)
                                {
                                    return false;
                                }
                            }

                            return true;
                        }
                    }
                    finally
                    {
                        _tempIgnoreRaycasts--;
                    }
            }

            return true;
        }

        public enum BlocksRaycasts
        {
            All,
            OutsideEnabledTutorialObjects,
            None,
        }
    }
}