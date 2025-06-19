using System;
using System.Text;
using TSQL;
using TSQL.Tokens;

namespace CnSharp.Sql
{
    public class TSqlFormatter
    {
        private readonly FormattingOptions _options;
        private int _parenthesisLevel;
        private bool _isInSubquery;
        private bool _isInCommaList;
        private bool _isInCaseStatement;
        private int _caseLevel;
        private bool _isInBetweenStatement;
        private bool _isInInList;
        private int _inListLevel;
        private bool _isInCte;
        private int _cteLevel;
        private int _nestingLevel;
        private int _baseIndentLevel;
        // private bool _isAfterClosingParen;
        private TSQLToken lastToken;

        public TSqlFormatter(FormattingOptions options = null)
        {
            _options = options ?? new FormattingOptions();
        }

        public string Format(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return sql;

            var tokens = TSQLTokenizer.ParseTokens(sql);
            var result = new StringBuilder();
            var lastTokenType = TSQLTokenType.Whitespace;
            _parenthesisLevel = 0;
            _isInSubquery = false;
            _isInCommaList = false;
            _isInCaseStatement = false;
            _caseLevel = 0;
            _isInBetweenStatement = false;
            _isInInList = false;
            _inListLevel = 0;
            _isInCte = false;
            _cteLevel = 0;
            _nestingLevel = 0;
            _baseIndentLevel = 0;

            foreach (var token in tokens)
            {
                // Handle line breaks for comments
                if (token.Type == TSQLTokenType.SingleLineComment || token.Type == TSQLTokenType.MultilineComment)
                {
                    if (lastTokenType != TSQLTokenType.Whitespace && lastTokenType != TSQLTokenType.SingleLineComment)
                    {
                        result.Append(' ');
                    }
                    result.Append(token.Text);
                    if (token.Type == TSQLTokenType.SingleLineComment)
                    {
                        result.AppendLine();
                        result.Append(GetIndentString());
                    }
                    lastTokenType = token.Type;
                    continue;
                }

                switch (token.Type)
                {
                    case TSQLTokenType.Keyword:
                        if (IsCaseStatementKeyword(token.Text))
                        {
                            HandleCaseStatementKeyword(token, result, ref lastTokenType);
                        }
                        else if (IsBetweenStatementKeyword(token.Text))
                        {
                            HandleBetweenStatementKeyword(token, result, ref lastTokenType);
                        }
                        else if (IsInListKeyword(token.Text))
                        {
                            HandleInListKeyword(token, result, ref lastTokenType);
                        }
                        else if (IsCteKeyword(token.Text))
                        {
                            HandleCteKeyword(token, result, ref lastTokenType);
                        }
                        else if (token.Text.Equals("SELECT", StringComparison.OrdinalIgnoreCase) && _parenthesisLevel > 0)
                        {
                            _isInSubquery = true;
                            _nestingLevel++;
                            result.AppendLine();
                            result.Append(GetIndentString());
                            result.Append(FormatKeywords(token));
                        }
                        else if (token.Text.Equals("FROM", StringComparison.OrdinalIgnoreCase))
                        {
                            result.AppendLine();
                            result.Append(GetIndentString());
                            result.Append(FormatKeywords(token));
                            result.Append(' ');
                        }
                        else if (token.Text.Equals("AS", StringComparison.OrdinalIgnoreCase))
                        {
                            // _isAfterSubqueryAs = true;
                            result.Append(' ');
                            result.Append(FormatKeywords(token));
                            result.Append(' ');
                        }
                        else if (token.Text.Equals("WHERE", StringComparison.OrdinalIgnoreCase))
                        {
                            result.AppendLine();
                            result.Append(GetIndentString(_baseIndentLevel));
                            result.Append(FormatKeywords(token));
                        }
                        else if (IsNewLineKeyword(token.Text))
                        {
                           
                                result.AppendLine();
                                result.Append(GetIndentString());
                            
                            result.Append(FormatKeywords(token));
                        }
                        else if (lastTokenType != TSQLTokenType.Whitespace)
                        {
                            result.Append(' ');
                            result.Append(FormatKeywords(token));
                        }
                        else
                        {
                            result.Append(FormatKeywords(token));
                        }
                        break;

                    case TSQLTokenType.Identifier:
                    case TSQLTokenType.SystemIdentifier:
                        if (lastTokenType != TSQLTokenType.Whitespace)
                        {
                            result.Append(' ');
                        }
                        result.Append(token.Text);
                        break;

                    case TSQLTokenType.StringLiteral:
                    case TSQLTokenType.NumericLiteral:
                    case TSQLTokenType.MoneyLiteral:
                    case TSQLTokenType.BinaryLiteral:
                        if (lastTokenType != TSQLTokenType.Whitespace)
                        {
                            result.Append(' ');
                        }
                        result.Append(token.Text);
                        break;

                    case TSQLTokenType.Operator:
                        if (token.Text == "(")
                        {
                            _parenthesisLevel++;
                            if (_isInInList && _inListLevel == 0)
                            {
                                result.AppendLine();
                                result.Append(GetIndentString());
                            }
                            else if (_isInSubquery)
                            {
                                result.AppendLine();
                                result.Append(GetIndentString());
                            }
                            result.Append(token.Text);
                            if (lastTokenType == TSQLTokenType.Keyword && lastToken.Text.Equals("FROM", StringComparison.OrdinalIgnoreCase))
                            {
                                
                                _baseIndentLevel = GetIndentLevel();
                                result.AppendLine();
                                result.Append(GetIndentString());
                            }
                        }
                        else if (token.Text == ")")
                        {
                            _parenthesisLevel--;
                            if (_isInInList && _inListLevel == 0)
                            {
                                _isInInList = false;
                                result.AppendLine();
                                result.Append(GetIndentString());
                            }
                            else if (_isInSubquery && _parenthesisLevel == 0)
                            {
                                _isInSubquery = false;
                                _nestingLevel--;
                                var subqueryIndentLevel = _baseIndentLevel;
                                result.AppendLine();
                                result.Append(GetIndentString(subqueryIndentLevel));
                            }
                            result.Append(token.Text);
                        }
                        else
                        {
                            result.Append(' ').Append(token.Text).Append(' ');
                        }
                        break;

                    case TSQLTokenType.Character:
                        if (token.Text == ",")
                        {
                            result.Append(token.Text);
                            if (_isInInList && _options.ExpandInLists)
                            {
                                result.AppendLine();
                                result.Append(GetIndentString());
                            }
                            else if (_options.ExpandCommaLists || _isInSubquery)
                            {
                                _isInCommaList = true;
                                result.AppendLine();
                                result.Append(GetIndentString());
                            }
                            else
                            {
                                result.Append(' ');
                            }
                        }
                        else if (token.Text == ".")
                        {
                            result.Append(token.Text);
                        }
                        else
                        {
                            result.Append(token.Text);
                        }
                        break;

                    case TSQLTokenType.Variable:
                    case TSQLTokenType.SystemVariable:
                        if (lastTokenType != TSQLTokenType.Whitespace)
                        {
                            result.Append(' ');
                        }
                        result.Append(token.Text);
                        break;

                    case TSQLTokenType.SystemColumnIdentifier:
                        if (lastTokenType != TSQLTokenType.Whitespace)
                        {
                            result.Append(' ');
                        }
                        result.Append(token.Text);
                        break;

                    case TSQLTokenType.Whitespace:
                        // Handle line breaks in whitespace
                        if (token.Text.Contains("\n"))
                        {
                            result.AppendLine();
                            result.Append(GetIndentString());
                        }
                        else
                        {
                            // Skip other whitespace as we're handling it ourselves
                        }
                        break;

                    case TSQLTokenType.IncompleteComment:
                    case TSQLTokenType.IncompleteIdentifier:
                    case TSQLTokenType.IncompleteString:
                        // Handle incomplete tokens by preserving them as-is
                        result.Append(token.Text);
                        break;

                    default:
                        result.Append(token.Text);
                        break;
                }

                lastTokenType = token.Type;
                lastToken = token;
            }

            return result.ToString().Trim();
        }

        private string FormatKeywords(TSQLToken token)
        {
            return _options.KeywordsUppercase.HasValue 
                ? (_options.KeywordsUppercase.Value ? token.Text.ToUpper() : token.Text.ToLower())
                : token.Text;
        }

        private string GetIndentString()
        {
            return GetIndentString(GetIndentLevel());
        }
        
        private string GetIndentString(int indentLevel)
        {
            return _options.IndentUseTab ? 
                new string('\t', indentLevel) : 
                new string(' ', indentLevel * _options.IndentSize);
        }

        private int GetIndentLevel()
        {
            var baseIndent = _nestingLevel;
            if (_isInCaseStatement) baseIndent += _caseLevel;
            if (_isInCte) baseIndent += _cteLevel;
            if (_isInSubquery) baseIndent += 1;
            if (_isInInList && _options.ExpandInLists) baseIndent += 1;
            if (_isInCommaList && _options.ExpandCommaLists) baseIndent += 1;
            return baseIndent;
        }

        private bool IsCaseStatementKeyword(string keyword)
        {
            var caseKeywords = new[] { "CASE", "WHEN", "THEN", "ELSE", "END" };
            return Array.Exists(caseKeywords, k => k.Equals(keyword, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsBetweenStatementKeyword(string keyword)
        {
            var betweenKeywords = new[] { "BETWEEN", "AND" };
            return Array.Exists(betweenKeywords, k => k.Equals(keyword, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsInListKeyword(string keyword)
        {
            return keyword.Equals("IN", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsCteKeyword(string keyword)
        {
            var cteKeywords = new[] { "WITH", "AS" };
            return Array.Exists(cteKeywords, k => k.Equals(keyword, StringComparison.OrdinalIgnoreCase));
        }

        private void HandleCaseStatementKeyword(TSQLToken token, StringBuilder result, ref TSQLTokenType lastTokenType)
        {
            var formattedKeyword = FormatKeywords(token);
            
            if(!_options.ExpandCaseStatements)
            {
                if (lastTokenType != TSQLTokenType.Whitespace)
                {
                    result.Append(' ');
                }
                result.Append(formattedKeyword);
                return;
            }

            switch (formattedKeyword.ToUpper())
            {
                case "CASE":
                    _isInCaseStatement = true;
                    _caseLevel++;
                    if (lastTokenType != TSQLTokenType.Whitespace)
                    {
                        result.Append(' ');
                    }
                    result.Append(formattedKeyword);
                    result.AppendLine();
                    result.Append(GetIndentString());
                    break;

                case "WHEN":
                    result.AppendLine();
                    result.Append(GetIndentString());
                    result.Append(formattedKeyword);
                    result.Append(' ');
                    break;

                case "THEN":
                    result.Append(' ');
                    result.Append(formattedKeyword);
                    result.AppendLine();
                    result.Append(GetIndentString());
                    break;

                case "ELSE":
                    result.AppendLine();
                    result.Append(GetIndentString());
                    result.Append(formattedKeyword);
                    result.AppendLine();
                    result.Append(GetIndentString());
                    break;

                case "END":
                    _caseLevel--;
                    if (_caseLevel == 0)
                    {
                        _isInCaseStatement = false;
                    }
                    result.AppendLine();
                    result.Append(GetIndentString());
                    result.Append(formattedKeyword);
                    break;
            }
        }

        private void HandleBetweenStatementKeyword(TSQLToken token, StringBuilder result, ref TSQLTokenType lastTokenType)
        {
            var keyword = token.Text;
            var formattedKeyword = FormatKeywords(token);

            switch (keyword.ToUpper())
            {
                case "BETWEEN":
                    _isInBetweenStatement = true;
                    if (lastTokenType != TSQLTokenType.Whitespace)
                    {
                        result.Append(' ');
                    }
                    result.Append(formattedKeyword);
                    if (_options.ExpandBetweenAndStatements)
                    {
                        result.AppendLine();
                        result.Append(GetIndentString());
                    }

                    break;

                case "AND":
                    if (_isInBetweenStatement)
                    {
                        if (_options.ExpandBetweenAndStatements)
                        {
                            result.AppendLine();
                            result.Append(GetIndentString());
                        }

                        result.Append(formattedKeyword);
                        result.Append(' ');
                        _isInBetweenStatement = false; 
                    }
                    else
                    {
                        // Handle regular AND operator
                        result.Append(' ').Append(formattedKeyword).Append(' ');
                    }
                    break;
            }
        }

        private void HandleInListKeyword(TSQLToken token, StringBuilder result, ref TSQLTokenType lastTokenType)
        {
            var formattedKeyword = FormatKeywords(token);

            if (lastTokenType != TSQLTokenType.Whitespace)
            {
                result.Append(' ');
            }
            result.Append(formattedKeyword);
            _isInInList = true;
            _inListLevel = 0;
        }

        private void HandleCteKeyword(TSQLToken token, StringBuilder result, ref TSQLTokenType lastTokenType)
        {
            var keyword = token.Text;
            var formattedKeyword = FormatKeywords(token);

            switch (keyword.ToUpper())
            {
                case "WITH":
                    _isInCte = true;
                    _cteLevel++;
                    if (lastTokenType != TSQLTokenType.Whitespace)
                    {
                        result.Append(' ');
                    }
                    result.Append(formattedKeyword);
                    result.AppendLine();
                    result.Append(GetIndentString());
                    break;

                case "AS":
                    if (_isInCte)
                    {
                        result.AppendLine();
                        result.Append(GetIndentString());
                        result.Append(formattedKeyword);
                        result.AppendLine();
                        result.Append(GetIndentString());
                    }
                    else
                    {
                        // Handle regular AS operator
                        result.Append(' ').Append(formattedKeyword).Append(' ');
                    }
                    break;
            }
        }

        private bool IsNewLineKeyword(string keyword)
        {
            var newLineKeywords = new[]
            {
                "SELECT", "FROM", "WHERE", "GROUP", "ORDER", "HAVING",
                "JOIN", "LEFT", "RIGHT", "INNER", "OUTER", "CROSS",
                "UNION", "INTERSECT", "EXCEPT", "WITH", "INSERT", "UPDATE",
                "DELETE", "MERGE", "VALUES", "SET", "INTO", "ON", "AND", "OR"
            };

            return Array.Exists(newLineKeywords, k => k.Equals(keyword, StringComparison.OrdinalIgnoreCase));
        }
    }
}