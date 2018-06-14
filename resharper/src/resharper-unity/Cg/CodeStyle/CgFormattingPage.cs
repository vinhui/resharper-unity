using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.Components;
using JetBrains.Application.UI.Options;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Feature.Services.OptionPages.CodeStyle;
using JetBrains.ReSharper.Feature.Services.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.CodeStyle
{
    [OptionsPage(
        PID,
        "Formatting Style",
        typeof(FeaturesEnvironmentOptionsThemedIcons.FormattingStyle),
        ParentId = CgPage.PID)]
    public class CgFormattingPage : CodeStylePage
    {
        public CgFormattingPage([NotNull] Lifetime lifetime, [NotNull] IContextBoundSettingsStoreLive smartContext, [NotNull] IUIApplication environment, [NotNull] ICodeStylePageSchema schema, [NotNull] CodeStylePreview preview)
            : base(lifetime, smartContext, environment, schema, preview)
        {
        }

        public const string PID = "CgCodeStyle";

        public override string Id => PID;
    }
}