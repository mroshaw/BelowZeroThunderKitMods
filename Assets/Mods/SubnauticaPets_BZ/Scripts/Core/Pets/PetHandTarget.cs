using static DaftAppleGames.SubnauticaPets.SubnauticaPetsPlugin;

namespace DaftAppleGames.SubnauticaPets.Pets
{
    /// <summary>
    ///     Template MonoBehaviour class. Use this to add new functionality and behaviours to
    ///     the game.
    /// </summary>
    internal class PetHandTarget : HandTarget, IHandTarget
    {
        // Useful pointer to pet component
        private Pet _pet;

        /// <summary>
        ///     Initialise the component
        /// </summary>
        private void Start()
        {
            _pet = GetComponent<Pet>();
            if (!_pet) ModDebugLog.LogError("PetHandTarget: GameObject MUST have a Pet component!");
        }

        /// <summary>
        ///     Handles a Mouse Hover over a pet
        /// </summary>
        public void OnHandHover(GUIHand hand)
        {
            if (!_pet) return;

            var main = HandReticle.main;

            // ModDebugLog.LogDebug($"OnHandOver... hand.IsFreeToInteract is: {hand.IsFreeToInteract()}");

            // Check for right mouse click
            if (GameInput.GetButtonDown(GameInput.Button.RightHand))
            {
                // Walk towards the player
                ModDebugLog.LogDebug("PetHandTarget: Walking to player...");
                _pet.MoveToPlayer();
                return;
            }

            // If hand is not free, allow the method to continue
            if (!hand.IsFreeToInteract()) return;

            // Set the cursor and cursor text
            main.SetIcon(HandReticle.IconType.Hand);
            if (_pet.IsDead)
            {
                main.SetText(HandReticle.TextType.Hand, $"The corpse of {_pet.PetName}", false);
            }
            else
            {
                main.SetText(HandReticle.TextType.Hand, $"Pet {_pet.PetName}", false, GameInput.Button.LeftHand);
                main.SetText(HandReticle.TextType.HandSubscript, $"Beckon {_pet.PetName}", false,
                    GameInput.Button.RightHand);
            }
        }

        /// <summary>
        ///     Handles a click on a pet
        /// </summary>
        public void OnHandClick(GUIHand hand)
        {
            if (!_pet) return;

            ModDebugLog.LogDebug("PetHandTarget: In OnHandClick");

            if (!hand.IsFreeToInteract() || _pet.IsDead) return;

            // Play random animation
            ModDebugLog.LogDebug("PetHandTarget: Playing animation...");
            _pet.PlayAnimation();
        }
    }
}