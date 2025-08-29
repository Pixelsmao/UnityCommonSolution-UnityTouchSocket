using System;
using System.Collections.Generic;
using System.Linq;

namespace SRDebugger.Editor
{
    internal abstract class IntegrityIssue
    {
        private readonly string _title;
        private readonly string _description;
        private List<Fix> _fixes;

        public string Title
        {
            get { return this._title; }
        }

        public string Description
        {
            get { return this._description; }
        }

        public IList<Fix> GetFixes()
        {
            if (this._fixes == null)
            {
                this._fixes = this.CreateFixes().ToList();
            }

            return this._fixes;
        }

        protected IntegrityIssue(string title, string description)
        {
            this._title = title;
            this._description = description;
        }

        protected abstract IEnumerable<Fix> CreateFixes();
    }

    internal abstract class Fix
    {
        private readonly string _name;
        private readonly string _description;
        private readonly bool _isAutoFix;

        public string Name
        {
            get { return this._name; }
        }

        public string Description
        {
            get { return this._description; }
        }

        public bool IsAutoFix
        {
            get { return this._isAutoFix; }
        }

        protected Fix(string name, string description, bool isAutoFix)
        {
            this._name = name;
            this._description = description;
            this._isAutoFix = isAutoFix;
        }

        public abstract void Execute();
    }

    internal class DelegateFix : Fix
    {
        private readonly Action _fixMethod;

        public DelegateFix(string name, string description, Action fixMethod) : base(name, description, true)
        {
            this._fixMethod = fixMethod;
        }

        public override void Execute()
        {
            this._fixMethod();
        }
    }
}