using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.Health
{
    // ─────────── Illness definitions (read-only data: Resources/Health/illnesses.json) ───────────

    /// <summary>
    /// One illness: what causes it, how it feels in the game (multipliers on the body's rates), and how it is treated —
    /// as in real life: over-the-counter medicine relieves symptoms (<see cref="relief"/>) and shortens it a little
    /// (<see cref="medicineShorten"/>); the doctor treats the cause (<see cref="clinicShorten"/>, more when seen within
    /// <see cref="earlyHours"/>, e.g. flu antivirals in the first 48 h); <see cref="sleepCures"/> ends it after a night.
    /// </summary>
    [Serializable]
    public sealed class IllnessDefinition
    {
        public string id, ja, kanji, vi, en, category, color, glyph;
        public string aboutVi, aboutEn, causeVi, causeEn, effectsVi, effectsEn, cureVi, cureEn, clinicVi, clinicEn, preventVi, preventEn;
        /// <summary>What to tell the doctor, and the clinic symptoms (clinic.json) it shows as.</summary>
        public string sayJa;
        public string[] symptoms = Array.Empty<string>();
        /// <summary>Game hours it lasts untreated (1 game hour = 1 real minute).</summary>
        public float hours = 24f;
        /// <summary>Health lost per real minute.</summary>
        public float healthPerMinute;
        public float move = 1f, energy = 1f, sleepiness = 1f, thirst = 1f, work = 1f;
        /// <summary>False: the body does not heal by itself while ill.</summary>
        public bool heals = true;
        /// <summary>SocialMood name of the moodlet, and the visual effect (sneeze, cough, fever, heat, dizzy, ache).</summary>
        public string mood, effect;
        /// <summary>Over-the-counter medicines (medicines[] ids) that help.</summary>
        public string[] medicines = Array.Empty<string>();
        public float relief, medicineShorten, clinicShorten, earlyHours, earlyClinicShorten;
        public bool sleepCures, stayHome, contagious;
        /// <summary>Chance to catch it per check (every 30 s) in <see cref="scenes"/> during <see cref="seasons"/>.</summary>
        public float chance;
        public string[] seasons = Array.Empty<string>();
        public string[] scenes = Array.Empty<string>();
        /// <summary>Game hours a vaccination protects (0 = no vaccine).</summary>
        public float immunityHours;

        public string Name => Loc.Pick(vi, en, kanji);
        public string About => Loc.Pick(aboutVi, aboutEn, aboutEn);
        public string Cause => Loc.Pick(causeVi, causeEn, causeEn);
        public string Effects => Loc.Pick(effectsVi, effectsEn, effectsEn);
        public string Cure => Loc.Pick(cureVi, cureEn, cureEn);
        public string Clinic => Loc.Pick(clinicVi, clinicEn, clinicEn);
        public string Prevent => Loc.Pick(preventVi, preventEn, preventEn);
        public Color Tint => ColorUtility.TryParseHtmlString(color, out var c) ? c : new Color(0.6f, 0.7f, 0.8f);

        public string CategoryName => category switch
        {
            "virus" => Loc.T("Virus · lây nhiễm", "Virus · infection", "ウイルス・感染症"),
            "work" => Loc.T("Lao động", "Work", "仕事"),
            "weather" => Loc.T("Thời tiết", "Weather", "天気"),
            "food" => Loc.T("Ăn uống", "Food", "食事"),
            "allergy" => Loc.T("Dị ứng", "Allergy", "アレルギー"),
            _ => Loc.T("Giấc ngủ", "Sleep", "睡眠"),
        };
    }

    /// <summary>An over-the-counter medicine sold at Hibari Mart (くすり shelf).</summary>
    [Serializable]
    public sealed class MedicineDefinition
    {
        public string id, ja, kanji, vi, en, glyph, useVi, useEn;
        public int price;
        /// <summary>Makes you sleepy (older antihistamines).</summary>
        public bool drowsy;
        public string Name => Loc.Pick(vi, en, ja);
        public string Use => Loc.Pick(useVi, useEn, useEn);
    }

    [Serializable]
    public sealed class IllnessCatalog
    {
        public const string ResourcePath = "Health/illnesses";
        public static readonly string[] Effects = { "sneeze", "cough", "fever", "heat", "dizzy", "ache" };
        public static readonly string[] Seasons = { "spring", "summer", "autumn", "winter" };

        public string schema;
        public IllnessDefinition[] illnesses = Array.Empty<IllnessDefinition>();
        public MedicineDefinition[] medicines = Array.Empty<MedicineDefinition>();

        private static IllnessCatalog _cached;

        public static IllnessCatalog Load()
        {
            if (_cached != null) return _cached;
            var text = Resources.Load<TextAsset>(ResourcePath);
            _cached = text != null ? Parse(text.text) : new IllnessCatalog();
            return _cached;
        }

        public static IllnessCatalog Parse(string json) => JsonUtility.FromJson<IllnessCatalog>(json) ?? new IllnessCatalog();

        public IllnessDefinition Illness(string id) => illnesses.FirstOrDefault(i => i.id == id);
        public MedicineDefinition Medicine(string id) => medicines.FirstOrDefault(m => m.id == id);

        /// <summary>Problems in the data (empty when the file is valid). Symptoms must exist in the clinic's catalog.</summary>
        public List<string> Validate(IEnumerable<string> clinicSymptoms)
        {
            var errors = new List<string>();
            var known = new HashSet<string>(clinicSymptoms ?? Array.Empty<string>());
            var ids = new HashSet<string>();
            foreach (var i in illnesses)
            {
                string at = "illness " + i.id + ": ";
                if (string.IsNullOrEmpty(i.id) || !ids.Add(i.id)) errors.Add("illness id missing or duplicate: " + i.id);
                if (string.IsNullOrEmpty(i.vi) || string.IsNullOrEmpty(i.en) || string.IsNullOrEmpty(i.kanji)) errors.Add(at + "missing names");
                if (string.IsNullOrEmpty(i.cureVi) || string.IsNullOrEmpty(i.causeVi) || string.IsNullOrEmpty(i.preventVi)) errors.Add(at + "missing cause / cure / prevention");
                if (i.hours <= 0f) errors.Add(at + "hours must be > 0");
                if (i.symptoms.Length == 0) errors.Add(at + "no symptoms");
                foreach (var s in i.symptoms) if (known.Count > 0 && !known.Contains(s)) errors.Add(at + "symptom " + s + " is not in clinic.json");
                foreach (var m in i.medicines) if (Medicine(m) == null) errors.Add(at + "unknown medicine " + m);
                if (!string.IsNullOrEmpty(i.effect) && !Effects.Contains(i.effect)) errors.Add(at + "unknown effect " + i.effect);
                foreach (var s in i.seasons) if (!Seasons.Contains(s)) errors.Add(at + "unknown season " + s);
                foreach (var v in new[] { i.relief, i.medicineShorten, i.clinicShorten, i.earlyClinicShorten }) if (v < 0f || v > 1f) errors.Add(at + "fractions must be 0…1");
                foreach (var v in new[] { i.move, i.energy, i.sleepiness, i.thirst, i.work }) if (v <= 0f) errors.Add(at + "multipliers must be > 0");
                if (i.chance > 0f && i.scenes.Length == 0) errors.Add(at + "a chance needs scenes where it is caught");
                foreach (var s in i.scenes) if (!WorldLocationCatalog.Has(s)) errors.Add(at + "unknown scene " + s + " (a scene name from WorldLocationCatalog)");
            }
            var medicineIds = new HashSet<string>();
            foreach (var m in medicines)
            {
                if (string.IsNullOrEmpty(m.id) || !medicineIds.Add(m.id)) errors.Add("medicine id missing or duplicate: " + m.id);
                if (m.price <= 0) errors.Add("medicine " + m.id + ": price");
                if (string.IsNullOrEmpty(m.ja) || string.IsNullOrEmpty(m.vi)) errors.Add("medicine " + m.id + ": names");
            }
            return errors;
        }
    }
}
