using System;
using System.Collections.Generic;
using UnityEngine;

namespace NihongoLife.Data
{
    [Serializable]
    public class RestaurantCategory
    {
        public string id;
        public string titleJa;
        public string titleReading;
        public string titleVi;
        public string titleEn;
    }

    [Serializable]
    public class RestaurantDish
    {
        public string id;
        public string categoryId;
        public string nameJa;
        public string reading;
        public string romaji;
        public string nameVi;
        public string nameEn;
        public int priceYen;
        public string descriptionJa;
        public string descriptionVi;
        public string descriptionEn;
        public List<string> ingredientsVi = new List<string>();
        public List<string> allergensVi = new List<string>();
        public string eatingTipVi;
        public bool isSpecialty;
        public string specialtyNoteVi;
        public string vocabularyTag;
    }

    [Serializable]
    public class RestaurantPhrase
    {
        public string ja;
        public string reading;
        public string romaji;
        public string vi;
        public string whenVi;
    }

    [Serializable]
    public class RestaurantEtiquette
    {
        public string titleJa;
        public string titleVi;
        public string bodyVi;
    }

    /// <summary>
    /// Data-driven restaurant menu. Everything shown by RestaurantMenuUI comes from this asset,
    /// so dishes, prices, phrases and etiquette notes can be edited in the Inspector (or the asset
    /// duplicated for another restaurant) without touching code or scenes.
    /// </summary>
    public class RestaurantMenuDefinition : ScriptableObject
    {
        public string id;
        public string restaurantNameJa;
        public string restaurantNameReading;
        public string restaurantNameVi;
        public string restaurantNameEn;
        public string taglineVi;
        public string taglineEn;
        public List<RestaurantCategory> categories = new List<RestaurantCategory>();
        public List<RestaurantDish> dishes = new List<RestaurantDish>();
        public List<RestaurantPhrase> phrases = new List<RestaurantPhrase>();
        public List<RestaurantEtiquette> etiquette = new List<RestaurantEtiquette>();

        public RestaurantDish FindDish(string dishId)
        {
            if (string.IsNullOrEmpty(dishId) || dishes == null) return null;
            return dishes.Find(d => d != null && d.id == dishId);
        }

        public List<RestaurantDish> GetDishesInCategory(string categoryId)
        {
            var result = new List<RestaurantDish>();
            if (dishes == null) return result;
            foreach (var dish in dishes)
            {
                if (dish != null && dish.categoryId == categoryId) result.Add(dish);
            }
            return result;
        }

        public List<RestaurantDish> GetSpecialties()
        {
            var result = new List<RestaurantDish>();
            if (dishes == null) return result;
            foreach (var dish in dishes)
            {
                if (dish != null && dish.isSpecialty) result.Add(dish);
            }
            return result;
        }
    }
}
