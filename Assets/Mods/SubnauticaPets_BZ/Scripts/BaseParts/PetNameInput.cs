using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DaftAppleGames.SubnauticaPets.BaseParts
{
    /// <summary>
    ///     Providers simple functionality on the Rename text control on the Pet Console
    /// </summary>
    public class PetNameInput : uGUI_InputGroup, IPointerHoverHandler
    {
        [SerializeField] private TMP_InputField petNameInput;
        [SerializeField] private RectTransform rt;
        [SerializeField] private float terminationSqrDistance = 4f;
        private Player _player;

        public override void Awake()
        {
            base.Awake();
            terminationSqrDistance = Mathf.Pow(3f, 2f);
        }

        public override void Update()
        {
            base.Update();
            if (focused && _player != null &&
                (_player.transform.position - rt.position).sqrMagnitude >= terminationSqrDistance)
                Deselect();
        }

        public void OnPointerHover(PointerEventData eventData)
        {
            if (enabled && selected)
            {
                HandReticle.main.SetText(HandReticle.TextType.Hand, string.Empty, true, GameInput.Button.LeftHand);
                HandReticle.main.SetText(HandReticle.TextType.HandSubscript, string.Empty, false);
                HandReticle.main.SetIcon(HandReticle.IconType.Interact);
            }
        }

        public override void OnSelect(bool lockMovement)
        {
            _player = Player.main;
            base.OnSelect(lockMovement);
        }

        public override void OnDeselect()
        {
            _player = null;
            base.OnDeselect();
        }
    }
}