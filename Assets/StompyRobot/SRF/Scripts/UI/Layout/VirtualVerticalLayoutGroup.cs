//#define PROFILE

namespace SRF.UI.Layout
{
    using Internal;
    using System;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    public interface IVirtualView
    {
        void SetDataContext(object data);
    }

    /// <summary>
    /// </summary>
    [AddComponentMenu(ComponentMenuPaths.VirtualVerticalLayoutGroup)]
    public class VirtualVerticalLayoutGroup : LayoutGroup, IPointerClickHandler
    {
        private readonly SRList<object> _itemList = new SRList<object>();
        private readonly SRList<int> _visibleItemList = new SRList<int>();

        private bool _isDirty = false;
        private readonly SRList<Row> _rowCache = new SRList<Row>();
        private ScrollRect _scrollRect;
        private int _selectedIndex;
        private object _selectedItem;

        [SerializeField] private SelectedItemChangedEvent _selectedItemChanged;

        private int _visibleItemCount;
        private readonly SRList<Row> _visibleRows = new SRList<Row>();
        public StyleSheet AltRowStyleSheet;
        public bool EnableSelection = true;
        public RectTransform ItemPrefab;

        /// <summary>
        /// Rows to show above and below the visible rect to reduce pop-in
        /// </summary>
        public int RowPadding = 2;

        public StyleSheet RowStyleSheet;
        public StyleSheet SelectedRowStyleSheet;

        /// <summary>
        /// Spacing to add between rows
        /// </summary>
        public float Spacing;

        /// <summary>
        /// If true, the scroll view will stick to the last element when fully scrolled to the bottom and an item is added
        /// </summary>
        public bool StickToBottom = true;

        public SelectedItemChangedEvent SelectedItemChanged
        {
            get { return this._selectedItemChanged; }
            set { this._selectedItemChanged = value; }
        }

        public object SelectedItem
        {
            get { return this._selectedItem; }
            set
            {
                if (this._selectedItem == value || !this.EnableSelection)
                {
                    return;
                }

                var newSelectedIndex = value == null ? -1 : this._itemList.IndexOf(value);

                // Ensure that the new selected item is present in the item list
                if (value != null && newSelectedIndex < 0)
                {
                    throw new InvalidOperationException("Cannot select item not present in layout");
                }

                // Invalidate old selected item row
                if (this._selectedItem != null)
                {
                    this.InvalidateItem(this._selectedIndex);
                }

                this._selectedItem = value;
                this._selectedIndex = newSelectedIndex;

                // Invalidate the newly selected item
                if (this._selectedItem != null)
                {
                    this.InvalidateItem(this._selectedIndex);
                }

                this.SetDirty();

                if (this._selectedItemChanged != null)
                {
                    this._selectedItemChanged.Invoke(this._selectedItem);
                }
            }
        }

        public override float minHeight
        {
            get { return this._itemList.Count * this.ItemHeight + this.padding.top + this.padding.bottom + this.Spacing * this._itemList.Count; }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!this.EnableSelection)
            {
                return;
            }

            var hitObject = eventData.pointerPressRaycast.gameObject;

            if (hitObject == null)
            {
                return;
            }

            var hitPos = hitObject.transform.position;
            var localPos = this.rectTransform.InverseTransformPoint(hitPos);
            var row = Mathf.FloorToInt(Mathf.Abs(localPos.y) / this.ItemHeight);

            if (row >= 0 && row < this._itemList.Count)
            {
                this.SelectedItem = this._itemList[row];
            }
            else
            {
                this.SelectedItem = null;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            this.ScrollRect.onValueChanged.AddListener(this.OnScrollRectValueChanged);

            var view = this.ItemPrefab.GetComponent(typeof(IVirtualView));

            if (view == null)
            {
                Debug.LogWarning(
                    "[VirtualVerticalLayoutGroup] ItemPrefab does not have a component inheriting from IVirtualView, so no data binding can occur");
            }
        }

        private void OnScrollRectValueChanged(Vector2 d)
        {
            if (d.y < 0 || d.y > 1)
            {
                this._scrollRect.verticalNormalizedPosition = Mathf.Clamp01(d.y);
            }

            //CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
            this.SetDirty();
        }

        protected override void Start()
        {
            base.Start();
            this.ScrollUpdate();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            this.SetDirty();
        }

        protected void Update()
        {
            if (!this.AlignBottom && !this.AlignTop)
            {
                Debug.LogWarning("[VirtualVerticalLayoutGroup] Only Lower or Upper alignment is supported.", this);
                this.childAlignment = TextAnchor.UpperLeft;
            }

            if (this.SelectedItem != null && !this._itemList.Contains(this.SelectedItem))
            {
                this.SelectedItem = null;
            }

            if (this._isDirty)
            {
                this._isDirty = false;
                this.ScrollUpdate();
            }
        }

        /// <summary>
        /// Invalidate a single row (before removing, or changing selection status)
        /// </summary>
        /// <param name="itemIndex"></param>
        protected void InvalidateItem(int itemIndex)
        {
            if (!this._visibleItemList.Contains(itemIndex))
            {
                return;
            }

            this._visibleItemList.Remove(itemIndex);

            for (var i = 0; i < this._visibleRows.Count; i++)
            {
                if (this._visibleRows[i].Index == itemIndex)
                {
                    this.RecycleRow(this._visibleRows[i]);
                    this._visibleRows.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>
        /// After removing or inserting a row, ensure that the cached indexes (used for layout) match up
        /// with the item index in the list
        /// </summary>
        protected void RefreshIndexCache()
        {
            for (var i = 0; i < this._visibleRows.Count; i++)
            {
                this._visibleRows[i].Index = this._itemList.IndexOf(this._visibleRows[i].Data);
            }
        }

        protected void ScrollUpdate()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            //Debug.Log("[SRConsole] ScrollUpdate {0}".Fmt(Time.frameCount));

            var pos = this.rectTransform.anchoredPosition;
            var startY = pos.y;

            var viewHeight = ((RectTransform)this.ScrollRect.transform).rect.height;

            // Determine the range of rows that should be visible
            var rowRangeLower = Mathf.FloorToInt(startY / (this.ItemHeight + this.Spacing));
            var rowRangeHigher = Mathf.CeilToInt((startY + viewHeight) / (this.ItemHeight + this.Spacing));

            // Apply padding to reduce pop-in
            rowRangeLower -= this.RowPadding;
            rowRangeHigher += this.RowPadding;

            rowRangeLower = Mathf.Max(0, rowRangeLower);
            rowRangeHigher = Mathf.Min(this._itemList.Count, rowRangeHigher);

            var isDirty = false;

#if PROFILE
			Profiler.BeginSample("Visible Rows Cull");
#endif

            for (var i = 0; i < this._visibleRows.Count; i++)
            {
                var row = this._visibleRows[i];

                // Move on if row is still visible
                if (row.Index >= rowRangeLower && row.Index <= rowRangeHigher)
                {
                    continue;
                }

                this._visibleItemList.Remove(row.Index);
                this._visibleRows.Remove(row);
                this.RecycleRow(row);
                isDirty = true;
            }

#if PROFILE
			Profiler.EndSample();
			Profiler.BeginSample("Item Visible Check");
#endif

            for (var i = rowRangeLower; i < rowRangeHigher; ++i)
            {
                if (i >= this._itemList.Count)
                {
                    break;
                }

                // Move on if row is already visible
                if (this._visibleItemList.Contains(i))
                {
                    continue;
                }

                var row = this.GetRow(i);
                this._visibleRows.Add(row);
                this._visibleItemList.Add(i);
                isDirty = true;
            }

#if PROFILE
			Profiler.EndSample();
#endif

            // If something visible has explicitly been changed, or the visible row count has changed
            if (isDirty || this._visibleItemCount != this._visibleRows.Count)
            {
                //Debug.Log("[SRConsole] IsDirty {0}".Fmt(Time.frameCount));
                LayoutRebuilder.MarkLayoutForRebuild(this.rectTransform);
            }

            this._visibleItemCount = this._visibleRows.Count;
        }

        public override void CalculateLayoutInputVertical()
        {
            this.SetLayoutInputForAxis(this.minHeight, this.minHeight, -1, 1);
        }

        public override void SetLayoutHorizontal()
        {
            var width = this.rectTransform.rect.width - this.padding.left - this.padding.right;

            // Position visible rows at 0 x
            for (var i = 0; i < this._visibleRows.Count; i++)
            {
                var item = this._visibleRows[i];

                this.SetChildAlongAxis(item.Rect, 0, this.padding.left, width);
            }

            // Hide non-active rows to one side. More efficient than enabling/disabling them
            for (var i = 0; i < this._rowCache.Count; i++)
            {
                var item = this._rowCache[i];

                this.SetChildAlongAxis(item.Rect, 0, -width - this.padding.left, width);
            }
        }

        public override void SetLayoutVertical()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            //Debug.Log("[SRConsole] SetLayoutVertical {0}".Fmt(Time.frameCount));

            // Position visible rows by the index of the item they represent
            for (var i = 0; i < this._visibleRows.Count; i++)
            {
                var item = this._visibleRows[i];

                this.SetChildAlongAxis(item.Rect, 1, item.Index * this.ItemHeight + this.padding.top + this.Spacing * item.Index, this.ItemHeight);
            }
        }

        private new void SetDirty()
        {
            base.SetDirty();

            if (!this.IsActive())
            {
                return;
            }

            this._isDirty = true;
            //CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
        }

        [Serializable]
        public class SelectedItemChangedEvent : UnityEvent<object> { }

        [Serializable]
        private class Row
        {
            public object Data;
            public int Index;
            public RectTransform Rect;
            public StyleRoot Root;
            public IVirtualView View;
        }

        #region Public Data Methods

        public void AddItem(object item)
        {
            this._itemList.Add(item);
            this.SetDirty();

            if (this.StickToBottom && Mathf.Approximately(this.ScrollRect.verticalNormalizedPosition, 0f))
            {
                this.ScrollRect.normalizedPosition = new Vector2(0, 0);
            }
        }

        public void RemoveItem(object item)
        {
            if (this.SelectedItem == item)
            {
                this.SelectedItem = null;
            }

            var index = this._itemList.IndexOf(item);

            this.InvalidateItem(index);
            this._itemList.Remove(item);

            this.RefreshIndexCache();

            this.SetDirty();
        }

        public void ClearItems()
        {
            for (var i = this._visibleRows.Count - 1; i >= 0; i--)
            {
                this.InvalidateItem(this._visibleRows[i].Index);
            }

            this._itemList.Clear();
            this.SetDirty();
        }

        #endregion

        #region Internal Properties

        private ScrollRect ScrollRect
        {
            get
            {
                if (this._scrollRect == null)
                {
                    this._scrollRect = this.GetComponentInParent<ScrollRect>();
                }

                return this._scrollRect;
            }
        }

        private bool AlignBottom
        {
            get
            {
                return this.childAlignment == TextAnchor.LowerRight || this.childAlignment == TextAnchor.LowerCenter ||
                       this.childAlignment == TextAnchor.LowerLeft;
            }
        }

        private bool AlignTop
        {
            get
            {
                return this.childAlignment == TextAnchor.UpperLeft || this.childAlignment == TextAnchor.UpperCenter ||
                       this.childAlignment == TextAnchor.UpperRight;
            }
        }

        private float _itemHeight = -1;

        private float ItemHeight
        {
            get
            {
                if (this._itemHeight <= 0)
                {
                    var layoutElement = this.ItemPrefab.GetComponent(typeof(ILayoutElement)) as ILayoutElement;

                    if (layoutElement != null)
                    {
                        this._itemHeight = layoutElement.preferredHeight;
                    }
                    else
                    {
                        this._itemHeight = this.ItemPrefab.rect.height;
                    }

                    if (this._itemHeight.ApproxZero())
                    {
                        Debug.LogWarning(
                            "[VirtualVerticalLayoutGroup] ItemPrefab must have a preferred size greater than 0");
                        this._itemHeight = 10;
                    }
                }

                return this._itemHeight;
            }
        }

        #endregion

        #region Row Pooling and Provisioning

        private Row GetRow(int forIndex)
        {
            // If there are no rows available in the cache, create one from scratch
            if (this._rowCache.Count == 0)
            {
                var newRow = this.CreateRow();
                this.PopulateRow(forIndex, newRow);
                return newRow;
            }

            var data = this._itemList[forIndex];

            Row row = null;
            Row altRow = null;

            // Determine if the row we're looking for is an alt row
            var target = forIndex % 2;

            // Try and find a row which previously had this data, so we can reuse it
            for (var i = 0; i < this._rowCache.Count; i++)
            {
                row = this._rowCache[i];

                // If this row previously represented this data, just use that one.
                if (row.Data == data)
                {
                    this._rowCache.RemoveAt(i);
                    this.PopulateRow(forIndex, row);
                    break;
                }

                // Cache a row which is was the same alt state as the row we're looking for, in case
                // we don't find an exact match.
                if (row.Index % 2 == target)
                {
                    altRow = row;
                }

                // Didn't match, reset to null
                row = null;
            }

            // If an exact match wasn't found, but a row with the same alt-status was found, use that one.
            if (row == null && altRow != null)
            {
                this._rowCache.Remove(altRow);
                row = altRow;
                this.PopulateRow(forIndex, row);
            }
            else if (row == null)
            {
                // No match found, use the last added item in the cache
                row = this._rowCache.PopLast();
                this.PopulateRow(forIndex, row);
            }

            return row;
        }

        private void RecycleRow(Row row)
        {
            this._rowCache.Add(row);
        }

        private void PopulateRow(int index, Row row)
        {
            row.Index = index;

            // Set data context on row
            row.Data = this._itemList[index];
            row.View.SetDataContext(this._itemList[index]);

            // If we're using stylesheets
            if (this.RowStyleSheet != null || this.AltRowStyleSheet != null || this.SelectedRowStyleSheet != null)
            {
                // If there is a selected row stylesheet, and this is the selected row, use that one
                if (this.SelectedRowStyleSheet != null && this.SelectedItem == row.Data)
                {
                    row.Root.StyleSheet = this.SelectedRowStyleSheet;
                }
                else
                {
                    // Otherwise just use the stylesheet suitable for the row alt-status
                    row.Root.StyleSheet = index % 2 == 0 ? this.RowStyleSheet : this.AltRowStyleSheet;
                }
            }
        }

        private Row CreateRow()
        {
            var item = new Row();

            var row = SRInstantiate.Instantiate(this.ItemPrefab);
            item.Rect = row;
            item.View = row.GetComponent(typeof(IVirtualView)) as IVirtualView;

            if (this.RowStyleSheet != null || this.AltRowStyleSheet != null || this.SelectedRowStyleSheet != null)
            {
                item.Root = row.gameObject.GetComponentOrAdd<StyleRoot>();
                item.Root.StyleSheet = this.RowStyleSheet;
            }

            row.SetParent(this.rectTransform, false);

            return item;
        }

        #endregion
    }
}
