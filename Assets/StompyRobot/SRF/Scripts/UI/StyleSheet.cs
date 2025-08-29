namespace SRF.UI
{
    using Helpers;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    [Serializable]
    public class Style
    {
        public Color ActiveColor = Color.white;
        public Color DisabledColor = Color.white;
        public Color HoverColor = Color.white;
        public Sprite Image;
        public Color NormalColor = Color.white;

        public Style Copy()
        {
            var s = new Style();
            s.CopyFrom(this);
            return s;
        }

        public void CopyFrom(Style style)
        {
            this.Image = style.Image;
            this.NormalColor = style.NormalColor;
            this.HoverColor = style.HoverColor;
            this.ActiveColor = style.ActiveColor;
            this.DisabledColor = style.DisabledColor;
        }
    }

    [Serializable]
    public class StyleSheet : ScriptableObject
    {
        [SerializeField] private List<string> _keys = new List<string>();

        [SerializeField] private List<Style> _styles = new List<Style>();

        [SerializeField] public StyleSheet Parent;

        public Style GetStyle(string key, bool searchParent = true)
        {
            var i = this._keys.IndexOf(key);

            if (i < 0)
            {
                if (searchParent && this.Parent != null)
                {
                    return this.Parent.GetStyle(key);
                }

                return null;
            }

            return this._styles[i];
        }

#if UNITY_EDITOR

        public int AddStyle(string key)
        {
            if (this._keys.Contains(key))
            {
                throw new ArgumentException("key already exists");
            }

            this._keys.Add(key);
            this._styles.Add(new Style());

            return this._keys.Count - 1;
        }

        public bool DeleteStyle(string key)
        {
            var i = this._keys.IndexOf(key);

            if (i < 0)
            {
                return false;
            }

            this._keys.RemoveAt(i);
            this._styles.RemoveAt(i);

            return true;
        }

        public IEnumerable<string> GetStyleKeys(bool includeParent = true)
        {
            if (this.Parent != null && includeParent)
            {
                return this._keys.Union(this.Parent.GetStyleKeys());
            }

            return this._keys.ToList();
        }

        [UnityEditor.MenuItem("Assets/Create/SRF/Style Sheet")]
        public static void CreateStyleSheet()
        {
            var o = AssetUtil.CreateAsset<StyleSheet>();
            AssetUtil.SelectAssetInProjectView(o);
        }

#endif
    }
}
