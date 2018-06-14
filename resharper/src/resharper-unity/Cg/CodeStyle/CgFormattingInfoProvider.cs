using JetBrains.ReSharper.Plugins.Unity.Cg.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl.CodeStyle;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.CodeStyle
{
    [Language(typeof(CgLanguage))]
    public class CgFormattingInfoProvider : FormatterInfoProviderWithFluentApi<CodeFormattingContext, CgCodeFormattingSettingsKey>
    {
        // ReSharper disable once EmptyConstructor
        public CgFormattingInfoProvider()
        {
            // TODO: formatting goes here
            // e.g.
            // Describe<FormattingRule>()
            //     .Group(...) [...]
        }
    }
}