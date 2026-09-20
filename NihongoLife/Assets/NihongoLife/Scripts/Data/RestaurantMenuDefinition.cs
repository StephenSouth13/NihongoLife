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

        [Header("Ordering and table service")]
        [Tooltip("Hunger restored (0-100 scale) per serving when eaten at the table.")]
        public float hungerRestore;
        [Tooltip("Thirst restored (0-100 scale) per serving when consumed at the table.")]
        public float thirstRestore;
        [Tooltip("What one order contains, e.g. 2 pieces. Shown next to the price when ordering.")]
        public string servingVi;
        [Tooltip("Model placed on the table when served. Empty = a simple placeholder shape.")]
        public GameObject servedModel;
        [Tooltip("Longest edge of the served model in meters. The model is fitted to this size at runtime, so it does not matter how the FBX was imported.")]
        public float servedModelSize = 0.12f;
        [Tooltip("Rotation applied to the model before fitting (kit models import lying down, so they use -90, 0, 0).")]
        public Vector3 servedModelEuler = new Vector3(-90f, 0f, 0f);
        [Tooltip("Serve on a plate (see RestaurantMenuDefinition.plateModel).")]
        public bool servedOnPlate;
    }

    /// <summary>One line the staff says during table service. Placeholders: {total}, {order}.</summary>
    [Serializable]
    public class RestaurantServiceLine
    {
        public string key;
        public string ja;
        public string reading;
        public string vi;
        public string en;
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

        [Header("Table service")]
        public string staffNameJa = "青木";
        public string staffNameVi = "Aoki";
        public List<RestaurantServiceLine> serviceLines = new List<RestaurantServiceLine>();
        [Tooltip("Plate model used under dishes that have servedOnPlate enabled.")]
        public GameObject plateModel;
        public float plateModelSize = 0.26f;
        public Vector3 plateModelEuler = new Vector3(-90f, 0f, 0f);

        public RestaurantServiceLine GetServiceLine(string key)
        {
            if (string.IsNullOrEmpty(key) || serviceLines == null) return null;
            return serviceLines.Find(line => line != null && line.key == key);
        }

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
