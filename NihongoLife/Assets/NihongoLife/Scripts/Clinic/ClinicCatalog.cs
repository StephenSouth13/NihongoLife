using System;
using System.Linq;
using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.Clinic
{
    [Serializable]
    public sealed class ClinicSymptom
    {
        public string id, sayJa, reading, vi, en;
        public string diagnosisJa, diagnosisVi, diagnosisEn, medicineJa, medicineVi, medicineEn;
        public string dosage;
        /// <summary>A cold symptom (what a soaked player has).</summary>
        public bool cold;
        public string Meaning => Loc.Pick(vi, en, sayJa);
        public string Diagnosis => Loc.Pick(diagnosisVi, diagnosisEn, diagnosisJa);
        public string Medicine => Loc.Pick(medicineVi, medicineEn, medicineJa);
    }

    [Serializable]
    public sealed class ClinicDosage
    {
        public string id, ja, vi, en;
        public string Meaning => Loc.Pick(vi, en, ja);
    }

    [Serializable]
    public sealed class ClinicDirection
    {
        public string id, ja, vi, en;
        public int angle;
        public string Meaning => Loc.Pick(vi, en, ja);
    }

    /// <summary>A townsperson who comes to the clinic on their own (ClinicPatientFlow): model, age and look
    /// (CharacterLook: elders grey, walk and talk slower), and what they come with (a symptom id).</summary>
    [Serializable]
    public sealed class ClinicPatient
    {
        public string id, model, nameJa, nameVi, hair = "natural", beard = "none", symptom;
        public int age = 30;
    }

    /// <summary>Clinic content (Resources/Clinic/clinic.json): symptoms, diagnoses, medicines, dosages, the
    /// health-check heights and eye-test directions. Add a symptom or a dosage there; nothing else changes.</summary>
    [Serializable]
    public sealed class ClinicCatalog
    {
        public const string ResourcePath = "Clinic/clinic";
        public string schema;
        public int visitFeeYen = 600;
        public string feeNoteVi, feeNoteEn, feeNoteJa;
        public ClinicSymptom[] symptoms = Array.Empty<ClinicSymptom>();
        public ClinicDosage[] dosages = Array.Empty<ClinicDosage>();
        public int[] heights = Array.Empty<int>();
        public ClinicDirection[] eyeDirections = Array.Empty<ClinicDirection>();
        /// <summary>The waiting room's own life: who comes, how often, what the staff say.</summary>
        public ClinicPatient[] patients = Array.Empty<ClinicPatient>();
        public float patientEverySeconds = 14f;
        public int maxPatients = 4;
        public string callJa, callVi, greetJa, doctorJa, byeJa, byeVi;

        public string FeeNote => Loc.Pick(feeNoteVi, feeNoteEn, feeNoteJa);

        private static ClinicCatalog _cached;

        public static ClinicCatalog Load()
        {
            if (_cached != null) return _cached;
            var text = Resources.Load<TextAsset>(ResourcePath);
            _cached = text != null ? JsonUtility.FromJson<ClinicCatalog>(text.text) : new ClinicCatalog();
            return _cached;
        }

        public ClinicSymptom Symptom(string id) => symptoms.FirstOrDefault(s => s.id == id);
        public ClinicDosage Dosage(string id) => dosages.FirstOrDefault(d => d.id == id);
    }
}
