using JetBrains.Annotations;
using JetBrains.Application.Components;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Feature.Services.OptionPages;
using JetBrains.ReSharper.Feature.Services.OptionPages.CodeStyle;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.CodeStyle
{
    [FormattingSettingsPresentationComponent]
    public class CgFormattingPageSchema : IndentStylePageSchema<CgCodeFormattingSettingsKey, CgCodeStylePreview>
    {
        public CgFormattingPageSchema([NotNull] Lifetime lifetime, [NotNull] IContextBoundSettingsStoreLive smartContext, [NotNull] IValueEditorViewModelFactory itemViewModelFactory, [NotNull] IComponentContainer container, ISettingsToHide settingsToHide): base(lifetime, smartContext, itemViewModelFactory, container, settingsToHide)
        {
        }

        public override string PageName => "Formatting Style";

        protected override Pair<string, PreviewParseType> GetPreviewForIndents()
        {
            return Pair.Of(
                "#pragma surface surf Lambert" + NL +		    		    
                "struct Input {" + NL +
                "float4 color : COLOR;" + NL +
                "};" + NL +
                "void surf (Input IN, inout SurfaceOutput o) {" + NL +
                "float f;" + NL +
                "float b = f;" + NL +
                "o.Albedo = 1;" + NL +
                "}",
                PreviewParseType.File
            );
        }

        public override KnownLanguage Language => CgLanguage.Instance;
    }
}