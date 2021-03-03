﻿//*********************************************************************
//CAD+ Toolset
//Copyright(C) 2020 Xarial Pty Limited
//Product URL: https://cadplus.xarial.com
//License: https://cadplus.xarial.com/license/
//*********************************************************************

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xarial.CadPlus.Batch.InApp.UI;
using Xarial.XCad;
using Xarial.XCad.Base.Attributes;
using Xarial.XCad.Documents;
using Xarial.XCad.UI.PropertyPage.Attributes;
using Xarial.XCad.UI.PropertyPage.Base;
using Xarial.XCad.UI.PropertyPage.Services;
using Xarial.XCad.UI.PropertyPage.Enums;
using Xarial.CadPlus.Batch.InApp.Properties;
using Xarial.CadPlus.Common.Services;
using Xarial.XToolkit.Wpf.Utils;
using Xarial.CadPlus.Batch.InApp.ViewModels;
using Xarial.CadPlus.Common.Attributes;
using Xarial.CadPlus.Plus.Attributes;
using Xarial.CadPlus.Plus.Services;
using Xarial.CadPlus.Plus.UI;
using Xarial.CadPlus.Plus.Modules;
using Xarial.CadPlus.Plus.Data;
using Xarial.XCad.Documents.Extensions;
using Xarial.XToolkit.Wpf.Extensions;

namespace Xarial.CadPlus.Batch.InApp
{
    public class SelectionScopeDependencyHandler : IDependencyHandler
    {
        public void UpdateState(IXApplication app, IControl source, IControl[] dependencies)
        {
            var scope = dependencies.First()?.GetValue();

            if (scope is InputScope_e) 
            {
                source.Visible = (InputScope_e)scope == InputScope_e.Selection; 
            }
        }
    }

    public class ReferencesScopeDependencyHandler : IDependencyHandler
    {
        public void UpdateState(IXApplication app, IControl source, IControl[] dependencies)
        {
            var scope = dependencies.First()?.GetValue();

            if (scope is InputScope_e)
            {
                source.Visible = (InputScope_e)scope != InputScope_e.Selection;
            }
        }
    }

    public enum InputScope_e
    {
        [Title("Selected Components")]
        Selection,

        [Title("Top Level Referenced Documents")]
        TopLevelReferences,

        [Title("All Referenced Documents")]
        AllReferences
    }

    public class ReferenceDocumentsVM : INotifyPropertyChanged
    {
        public ICadEntityDescriptor EntityDescriptor { get; }
        public IXDocument[] AllReferences => m_AllReferences.Value;
        public IXDocument[] TopLevelReferences => m_TopLevelReferences.Value;

        private Lazy<IXDocument[]> m_AllReferences;
        private Lazy<IXDocument[]> m_TopLevelReferences;
        private bool m_TopLevelOnly;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool TopLevelOnly 
        {
            get => m_TopLevelOnly;
            set 
            {
                m_TopLevelOnly = value;
                this.NotifyChanged();
            }
        }

        public void SetDocument(IXDocument doc, bool topLevelOnly) 
        {
            m_TopLevelOnly = topLevelOnly;
            m_TopLevelReferences = new Lazy<IXDocument[]>(() => new IXDocument[] { doc }.Union(doc.Dependencies).ToArray());
            m_AllReferences = new Lazy<IXDocument[]>(() => new IXDocument[] { doc }.Union(doc.GetAllDependencies()).ToArray());
        }

        public ReferenceDocumentsVM(ICadEntityDescriptor cadEntDesc) 
        {
            EntityDescriptor = cadEntDesc;
        }
    }

    [IconEx(typeof(Resources), nameof(Resources.batch_plus_assm_vector), nameof(Resources.batch_plus_assm_icon))]
    [Title("Batch+")]
    public class AssemblyBatchData
    {
        public class InputGroup 
        {
            [ControlOptions(height: 100)]
            [DependentOn(typeof(SelectionScopeDependencyHandler), nameof(Scope))]
            [Description("List of components to run macros on")]
            public List<IXComponent> Components { get; set; }

            [ControlOptions(height: 100)]
            [DependentOn(typeof(ReferencesScopeDependencyHandler), nameof(Scope))]
            [Description("All referenced docyments")]
            [StandardControlIcon(BitmapLabelType_e.SelectComponent)]
            [CustomControl(typeof(ReferencesList))]
            public ReferenceDocumentsVM AllDocuments { get; }

            [OptionBox]
            [ControlOptions(align: ControlLeftAlign_e.Indent)]
            [ControlTag(nameof(Scope))]
            public InputScope_e Scope 
            {
                get => m_Scope;
                set 
                {
                    m_Scope = value;

                    if (m_Scope == InputScope_e.AllReferences || m_Scope == InputScope_e.TopLevelReferences) 
                    {
                        AllDocuments.TopLevelOnly = m_Scope == InputScope_e.TopLevelReferences;
                    }
                }
            }

            private IXDocument m_Document;
            private InputScope_e m_Scope;

            [ExcludeControl]
            internal IXDocument Document 
            {
                get => m_Document;
                set 
                {
                    m_Document = value;
                    
                    Components = m_Document.Selections.OfType<IXComponent>().ToList();
                    AllDocuments.SetDocument(value, Scope == InputScope_e.TopLevelReferences);
                }
            }

            public InputGroup(ICadEntityDescriptor cadEntDesc) 
            {
                AllDocuments = new ReferenceDocumentsVM(cadEntDesc);
            }
        }

        public class MacrosGroup 
        {
            [CustomControl(typeof(MacrosList))]
            [ControlOptions(height: 100)]
            [IconEx(typeof(Resources), nameof(Resources.macros_vector), nameof(Resources.macros_icon))]
            public MacrosVM Macros { get; set; }

            [Title("Add Macros...")]
            [ControlOptions(align: ControlLeftAlign_e.Indent)]
            public Action AddMacros { get; }

            public MacrosGroup(FileTypeFilter[] macroFilters) 
            {
                AddMacros = OnAddMacros;

                Macros = new MacrosVM(macroFilters);
            }

            private void OnAddMacros()
            {
                Macros.RequestAddMacros();
            }
        }

        public class OptionsGroup 
        {
            [Description("Open each document in its own window (activate)")]
            [Title("Activate Documents")]
            [ControlOptions(align: ControlLeftAlign_e.Indent)]
            public bool ActivateDocuments { get; set; } = true;

            [Description("Allow opening documents which are not currently loaded into memory as read-only")]
            [Title("Allow Read-Only")]
            [ControlOptions(align: ControlLeftAlign_e.Indent)]
            public bool AllowReadOnly { get; set; } = false;

            [Description("Allow opening documents which are not currently loaded into memory in a rapid mode")]
            [Title("Allow Rapid")]
            [ControlOptions(align: ControlLeftAlign_e.Indent)]
            public bool AllowRapid { get; set; } = false;

            [DynamicControls(Group_e.Options)]
            public List<IRibbonCommand> AdditionalCommands { get; }

            public OptionsGroup() 
            {
                AdditionalCommands = new List<IRibbonCommand>();
            }
        }

        public InputGroup Input { get; }
        public MacrosGroup Macros { get; }
        public OptionsGroup Options { get; }

        public AssemblyBatchData(ICadEntityDescriptor cadEntDesc)
        {
            Input = new InputGroup(cadEntDesc);
            Macros = new MacrosGroup(cadEntDesc.MacroFileFilters);
            Options = new OptionsGroup();
        }
    }
}
