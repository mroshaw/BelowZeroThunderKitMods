using System;
using System.Collections.Generic;
using Nautilus.Crafting;
using UnityEngine;

namespace DaftAppleGames.ModUtils
{
    /// <summary>
    /// Class to help configure recipes in the Unity Inspector
    /// </summary>
    [CreateAssetMenu(fileName = "RecipePreset", menuName = "Mods/Recipe Preset", order = 1)]
    public class RecipePreset : ScriptableObject
    {
        [SerializeField] private List<RecipeHelperEntry> entries = new List<RecipeHelperEntry>();
        
        /// <summary>
        /// Get the list as RecipeData
        /// </summary>
        /// <returns></returns>
        public RecipeData GetAsRecipeData()
        {
            List<Ingredient> ingredients = new List<Ingredient>();
            
            foreach (RecipeHelperEntry entry in entries)
            {
                ingredients.Add(new Ingredient(entry.techType, entry.amount));
            }
            RecipeData recipeData = new RecipeData(ingredients);
            return recipeData;
        }
        
        [Serializable]
        private class RecipeHelperEntry
        {
            [SerializeField] internal TechType techType;
            [SerializeField] internal int amount;
        }
    }
}