using JetBrains.Application.Settings;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.Format;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.CodeStyle
{
    [SettingsKey(typeof(CodeFormattingSettingsKey), "Code formatting in Cg/HLSL")]
    public class CgCodeFormattingSettingsKey : FormatSettingsKeyBase
    {        
    }
}