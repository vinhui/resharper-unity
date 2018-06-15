using JetBrains.Application.CommandProcessing;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.ActionSystem.Text;
using JetBrains.DataFlow;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.TypingAssist;
using JetBrains.ReSharper.Plugins.Unity.Cg.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodeTypes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CachingLexers;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;
using JetBrains.TextControl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.TypingAssist
{
    // TODO: implement more
    [SolutionComponent]
    public class CgTypingAssist : TypingAssistForCLikeLanguage<CgLanguage>, ITypingHandler
    {
        public CgTypingAssist(Lifetime lifetime, ISolution solution, ISettingsStore settingsStore, CachingLexerService cachingLexerService, ICommandProcessor commandProcessor, IPsiServices psiServices, IExternalIntellisenseHost externalIntellisenseHost, ITypingAssistManager typingAssistManager)
            : base(solution, settingsStore, cachingLexerService, commandProcessor, psiServices, externalIntellisenseHost)
        {
            typingAssistManager.AddTypingHandler(lifetime, '{', this, HandleLeftBraceTyped, IsTypingSmartLBraceHandlerAvailable);
            typingAssistManager.AddTypingHandler(lifetime, '}', this, HandleRightBraceTyped, IsTypingHandlerAvailable);
            typingAssistManager.AddTypingHandler(lifetime, '(', this, HandleLeftBracketOrParenthTyped, IsTypingSmartParenthesisHandlerAvailable);
            typingAssistManager.AddTypingHandler(lifetime, '[', this, HandleLeftBracketOrParenthTyped, IsTypingSmartParenthesisHandlerAvailable);
            typingAssistManager.AddTypingHandler(lifetime, ']', this, HandleRightBracketTyped, IsTypingSmartParenthesisHandlerAvailable);
            typingAssistManager.AddTypingHandler(lifetime, ')', this, HandleRightBracketTyped, IsTypingSmartParenthesisHandlerAvailable);
//            typingAssistManager.AddTypingHandler(lifetime, '\'', this, HandleQuoteTyped, IsTypingSmartParenthesisHandlerAvailable);
            typingAssistManager.AddTypingHandler(lifetime, ';', this, HandleSemicolonTyped, IsTypingHandlerAvailable);
            typingAssistManager.AddTypingHandler(lifetime, '.', this, HandleDotTyped, IsTypingHandlerAvailable);
            
//            typingAssistManager.AddActionHandler(lifetime, TextControlActions.ENTER_ACTION_ID, this, HandleEnterPressed, IsActionHandlerAvailabile);
//            typingAssistManager.AddActionHandler(lifetime, TextControlActions.BACKSPACE_ACTION_ID, this, HandleBackspacePressed, IsActionHandlerAvailabile);
//            typingAssistManager.AddActionHandler(lifetime, TextControlActions.DELETE_ACTION_ID, this, HandleDelPressed, IsActionHandlerAvailabile);
            
            typingAssistManager.AddActionHandler(lifetime, TextControlActions.TAB_ACTION_ID, this, HandleTabPressed, IsActionHandlerAvailabile);
            typingAssistManager.AddActionHandler(lifetime, TabLeftActionHandler.ACTION_ID, this, HandleTabLeftPressed, IsActionHandlerAvailabile);
        }
        
        public bool QuickCheckAvailability(ITextControl textControl, IPsiSourceFile projectFile) => projectFile.LanguageType.Is<CgProjectFileType>();

        protected override bool IsSupported(ITextControl textControl)
        {
            var psiSourceFile = textControl.Document.GetPsiSourceFile(Solution);
            if (psiSourceFile == null || !psiSourceFile.LanguageType.Is<CgProjectFileType>() || !psiSourceFile.IsValid())
                return false;
            
            return psiSourceFile.Properties.ShouldBuildPsi;
        }

        protected override bool NeedSkipCloseBracket(ITextControl textControl, CachingLexer lexer, char charTyped)
        {
            var nextToken = lexer.TokenType;
            if ((charTyped == ')' && nextToken != RPARENTH) ||
                (charTyped == ']' && nextToken != RBRACKET) ||
                (charTyped == '}' && nextToken != RBRACE))
                return false;

            // find the leftmost non-closed bracket (excluding typed) of typed class so that there are no opened brackets of other type
            var bracketMatcher = CreateBracketMatcher();
            var searchTokenType = charTyped == ')'
                ? LPARENTH
                : charTyped == ']' ? LBRACKET : LBRACE;
            int? leftParenthPos = null;
            TokenNodeType tokenType;
            // Here we search for the farest left brace/bracket/parenthesis that is still unmatched at the cursor position (so we skip pairs)
            for (lexer.Advance(-1); (tokenType = lexer.TokenType) != null; lexer.Advance(-1))
            {
                // Don't put left braces to stack, because we go in reverse direction and need to match right with lefts, not lefts with rights
                if (bracketMatcher.Direction(tokenType) == 1 && bracketMatcher.IsStackEmpty())
                {
                    if (tokenType == searchTokenType)
                        leftParenthPos = lexer.CurrentPosition;
                }
                else if (!bracketMatcher.ProceedStack(tokenType))
                    break;
            }

            // proceed with search result
            if (leftParenthPos == null)
                return false;
            lexer.CurrentPosition = leftParenthPos.Value;
            return bracketMatcher.FindMatchingBracket(lexer);
        }

        protected override bool IsTokenNotSuitableForCloseParenth(TokenNodeType nextTokenType)
        {
            return nextTokenType != WHITE_SPACE
                   && nextTokenType != NEW_LINE
                   && nextTokenType != C_STYLE_COMMENT
                   && nextTokenType != END_OF_LINE_COMMENT
                   && nextTokenType != SEMICOLON
                   && nextTokenType != CgTokenNodeTypes.COMMA
                   && nextTokenType != RBRACKET
                   && nextTokenType != RBRACE
                   && nextTokenType != RPARENTH;
        }

        #region not implemented

        protected override SettingsScalarEntry GetStickCommentSettingEntry(IContextBoundSettingsStore bindedDataContext)
        {
            // return bindedDataContext.Schema.GetScalarEntry((JavaScriptCodeFormattingSettingsKey key) => key.STICK_COMMENT);
            return null;
        }

        protected override bool GetPreferWrapBeforeOpSignSetting(IContextBoundSettingsStore bindedDataContext)
        {
            return false;
        }

        protected override bool DoReformatForSmartEnter(ITextControl textControl, TreeOffset lBraceTreePos, TreeOffset rBraceTreePos, int charPos,
            ITokenNode lBraceNode, ITokenNode rBraceNode, bool afterLBrace, IFile file, bool oneLine)
        {
            return false;
        }
        
        protected override bool IsNodeSuitableAsSemicolonFormatParent(ITreeNode node)
        {
            return false;
        }

        protected override ITreeNode GetParentForFormatOnSemicolon(ITreeNode node)
        {
            return null;
        }

        protected override bool GetAutoinsertDataForRBrace(ITextControl textControl, ITokenNode rBraceToken, TreeTextRange treeLBraceRange,
            int lBracePos, int position, IDocument document, out TreeOffset positionForRBrace, out string rBraceText,
            ref IFile file)
        {
            positionForRBrace = rBraceToken.GetTreeTextRange().EndOffset;
            rBraceText = "}";
            return false;
        }
        
        public override Pair<ITreeRange, ITreeRangePointer> GetRangeToFormatAfterRBrace(ITextControl textControl) => Pair.Of(default(ITreeRange), default(ITreeRangePointer));

        #endregion
        
        // no string literals in Cg
        
        protected override NodeTypeSet GetQuoteCorrespondingTokenType(char c) => NodeTypeSet.Empty;
        protected override bool IsNextCharDoesntStartNewLiteral(ITypingContext typingContext, CachingLexer lexer, int charPos, IBuffer buffer) => false;
        protected override bool IsStopperTokenForStringLiteral(TokenNodeType tokenType) => false;
        
        protected override bool CheckThatCLikeLineEndsInOpenStringLiteral(ITextControl textControl, CachingLexer lexer,
            int lineEndPos, char c, NodeTypeSet correspondingTokenType, bool isStringWithAt, ref int charPos,
            bool defaultReturnValue) => false;
        
        protected override BracketMatcher CreateBracketMatcher() => new CgBracketMathcer();
        protected override BracketMatcher CreateBraceMatcher() => new CgBraceMatcher();

        protected override bool IsLBrace(ITextControl textControl, ITreeNode node) => node.GetTokenType() == CgTokenNodeTypes.LBRACE;
        protected override bool IsRBrace(ITextControl textControl, ITreeNode node) => node.GetTokenType() == CgTokenNodeTypes.RBRACE;
        protected override bool IsSemicolon(ITextControl textControl, ITreeNode node) => node.GetTokenType() == CgTokenNodeTypes.SEMICOLON;
        
        protected override bool IsLBrace(ITextControl textControl, CachingLexer lexer) => lexer.TokenType == CgTokenNodeTypes.LBRACE;
        protected override bool IsRBrace(ITextControl textControl, CachingLexer lexer) => lexer.TokenType == CgTokenNodeTypes.RBRACE;

        protected override TokenNodeType LBRACE => CgTokenNodeTypes.LBRACE;
        protected override TokenNodeType RBRACE => CgTokenNodeTypes.RBRACE; 
        protected override TokenNodeType LBRACKET => CgTokenNodeTypes.LBRACKET; 
        protected override TokenNodeType RBRACKET => CgTokenNodeTypes.RBRACKET; 
        protected override TokenNodeType LPARENTH => CgTokenNodeTypes.LPAREN; 
        protected override TokenNodeType RPARENTH => CgTokenNodeTypes.RPAREN; 
        protected override TokenNodeType WHITE_SPACE => CgTokenNodeTypes.WHITESPACE; 
        protected override TokenNodeType NEW_LINE => CgTokenNodeTypes.NEW_LINE; 
        protected override TokenNodeType END_OF_LINE_COMMENT => CgTokenNodeTypes.SINGLE_LINE_COMMENT; 
        protected override TokenNodeType C_STYLE_COMMENT => CgTokenNodeTypes.DELIMITED_COMMENT; 
        protected override TokenNodeType PLUS => CgTokenNodeTypes.PLUS; 
        protected override TokenNodeType SEMICOLON => CgTokenNodeTypes.SEMICOLON; 
        protected override TokenNodeType DOT => CgTokenNodeTypes.DOT;
        
        protected override NodeTypeSet STRING_LITERALS => NodeTypeSet.Empty;
        protected override NodeTypeSet ACCESS_CHAIN_TOKENS => new NodeTypeSet(CgTokenNodeTypes.DOT);

        private class CgBracketMathcer : BracketMatcher
        {
            public CgBracketMathcer() : base(new[]
            {
                Pair.Of(CgTokenNodeTypes.LBRACKET, CgTokenNodeTypes.RBRACKET),
                Pair.Of(CgTokenNodeTypes.LPAREN, CgTokenNodeTypes.RPAREN)
            })
            {
            }
        }
        
        private class CgBraceMatcher : BracketMatcher
        {
            public CgBraceMatcher()
                : base(new []{Pair.Of(CgTokenNodeTypes.LBRACE, CgTokenNodeTypes.RBRACE)})
            {
            }
        }
    }
}