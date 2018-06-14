using System.Collections.Generic;
using JetBrains.Application.UI.Options;
using JetBrains.Application.UI.Options.OptionsDialog;
using JetBrains.ReSharper.Feature.Services.OptionPages.CodeEditing;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.CodeStyle
{
    [OptionsPage(PID, "Cg", null, ParentId = CodeEditingPage.PID)]
    public class CgPage : AEmptyOptionsPage
    {
        public const string PID = "Cg";

        public override IEnumerable<string> GetTagKeywordsForPage()
        {
            return new [] {"Cg, HLSL, GLSL, Shader"};
        }
    }
}