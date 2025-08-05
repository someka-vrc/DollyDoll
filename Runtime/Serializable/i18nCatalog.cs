using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Somekasu.DollyDoll
{
    [CreateAssetMenu(fileName = "I18nCatalog", menuName = "Someka/i18n/Catalog")]
    public class I18nCatalog : ScriptableObject
    {
        [SerializeField]
        internal List<I18nCatalogEntry> Entries = new();
    }

    [Serializable]
    internal struct I18nCatalogEntry
    {
        [SerializeField]
        internal string Locale;
        [SerializeField]
        internal string DisplayName;
        [SerializeField]
        internal TextAsset Resource;

        internal readonly bool IsValid => !string.IsNullOrEmpty(Locale) && !string.IsNullOrEmpty(DisplayName) && Resource != null && Resource;

        internal I18nCatalogEntry(string locale, string displayName, TextAsset resource)
        {
            Locale = locale;
            DisplayName = displayName;
            Resource = resource;
        }

        public override readonly int GetHashCode() => HashCode.Combine(Locale, DisplayName, Resource);
    }
}