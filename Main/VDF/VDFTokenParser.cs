using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

public enum VDFTokenType
{
	//WiderMetadataEndMarker,
	//MetadataBaseValue,
	//LiteralStartMarker, // this is taken care of within the TokenParser class, so we don't need a passable-to-the-outside enum-value for it
	//LiteralEndMarker
	//DataPropName,
	//DataStartMarker,
	//PoppedOutDataStartMarker,
	//PoppedOutDataEndMarker,
	//ItemSeparator,
	//DataBaseValue,
	//DataEndMarker,
	//Indent,

	// helper tokens for token-parser (or reader)
	LiteralStartMarker,
	LiteralEndMarker,
	StringStartMarker,
	StringEndMarker,
	InLineComment,
	SpaceOrCommaSpan,

	None,
	Tab,
	LineBreak,

	Metadata,
	MetadataEndMarker,
	Key,
	KeyValueSeparator,
	PoppedOutChildGroupMarker,
	
	Null,
	Boolean,
	Number,
	String,
	ListStartMarker,
	ListEndMarker,
	MapStartMarker,
	MapEndMarker
}
public class VDFToken
{
	public VDFTokenType type;
	public int position;
	public int index;
	public string text;
	public VDFToken(VDFTokenType type, int position, int index, string text)
	{
		this.type = type;
		this.position = position;
		this.index = index;
		this.text = text;
	}

	public override string ToString() // for debugging
	{
		var result = "   " + text.Replace("\t", "\\t").Replace("\n", "\\n") + "   ";
		for (var i = result.Length; i < 30; i++)
			result += " ";
		return result;
	}
}
public static class VDFTokenParser
{
	static List<char> charsAToZ = new Regex(".").Matches("abcdefghijklmnopqrstuvwxyz").OfType<Match>().Select(a=>a.Value[0]).ToList();
	static HashSet<char> chars0To9DotAndNegative = new HashSet<char>(new Regex(".").Matches("0123456789.-+eE").OfType<Match>().Select(a=>a.Value[0]));
	
	public static List<VDFToken> ParseTokens(string text, VDFLoadOptions options = null, bool parseAllTokens = false, bool postProcessTokens = true)
	{
		text = (text ?? "").Replace("\r\n", "\n"); // maybe temp
		options = options ?? new VDFLoadOptions();

		var result = new List<VDFToken>();

		var currentTokenFirstCharPos = 0;
		var currentTokenTextBuilder = new StringBuilder();
		var currentTokenType = VDFTokenType.None;
		string activeLiteralStartChars = null;
		char? activeStringStartChar = null;
		string lastScopeIncreaseChar = null;
		bool addNextCharToTokenText = true;
		for (var i = 0; i < text.Length; i++)
		{
			char ch = text[i];
			char? nextChar = i + 1 < text.Length ? text[i + 1] : (char?)null;

			//int nextNonSpaceCharPos = FindNextNonXCharPosition(text, i + 1, ' ');
			//char? nextNonSpaceChar = nextNonSpaceCharPos != -1 ? text[nextNonSpaceCharPos] : (char?)null;

			if (addNextCharToTokenText)
				currentTokenTextBuilder.Append(ch);
			addNextCharToTokenText = true;

			if (activeLiteralStartChars != null) // if in literal
			{
				// if first char of literal-end-marker
				if (ch == '>' && i + activeLiteralStartChars.Length <= text.Length && text.Substring(i, activeLiteralStartChars.Length) == activeLiteralStartChars.Replace("<", ">"))
				{
					//currentTokenTextBuilder = new StringBuilder(activeLiteralStartChars.Replace("<", ">"));
					//currentTokenType = VDFTokenType.LiteralEndMarker;
					//i += currentTokenTextBuilder.Length - 1; // have next char processed be the one right after literal-end-marker

					if (parseAllTokens)
						result.Add(new VDFToken(VDFTokenType.LiteralEndMarker, i, result.Count, activeLiteralStartChars.Replace("<", ">"))); // (if this end-marker token is within a string, it'll come before the string token)
					currentTokenTextBuilder.Remove(currentTokenTextBuilder.Length - 1, 1); // remove current char from the main-token text
					currentTokenFirstCharPos += activeLiteralStartChars.Length; // don't count this inserted token text as part of main-token text
					i += activeLiteralStartChars.Length - 2; // have next char processed be the last char of literal-end-marker
					addNextCharToTokenText = false; // but don't have it be added to the main-token text

					if (text[i + 1 - activeLiteralStartChars.Length] == '#') // if there was a hash just before literal-end-marker (e.g. "some text>#>>"), remove it from main-token text
						currentTokenTextBuilder.Remove(currentTokenTextBuilder.Length - 1, 1); // remove current char from the main-token text

					activeLiteralStartChars = null;

					continue;
				}
			}
			else
			{
				if (ch == '<' && nextChar == '<') // if first char of literal-start-marker
				{
					activeLiteralStartChars = "";
					while (i + activeLiteralStartChars.Length < text.Length && text[i + activeLiteralStartChars.Length] == '<')
						activeLiteralStartChars += "<";

					//currentTokenTextBuilder = new StringBuilder(activeLiteralStartChars);
					//currentTokenType = VDFTokenType.LiteralStartMarker;
					//i += currentTokenTextBuilder.Length - 1; // have next char processed be the one right after comment (i.e. the line-break char)

					if (parseAllTokens)
						result.Add(new VDFToken(VDFTokenType.LiteralStartMarker, i, result.Count, activeLiteralStartChars));
					currentTokenTextBuilder.Remove(currentTokenTextBuilder.Length - 1, 1); // remove current char from the main-token text
					currentTokenFirstCharPos += activeLiteralStartChars.Length; // don't count this inserted token text as part of main-token text
					i += activeLiteralStartChars.Length - 1; // have next char processed be the one right after literal-start-marker

					if (text[i + 1] == '#') // if there is a hash just after literal-start-marker (e.g. "<<#<some text"), skip it
					{
						currentTokenFirstCharPos++;
						i++;
					}

					continue;
				}
				// else
				{
					if (activeStringStartChar == null)
					{
						if (ch == '\'' || ch == '"') // if char of string-start-marker
						{
							activeStringStartChar = ch;

							//currentTokenType = VDFTokenType.StringStartMarker;
							if (parseAllTokens)
								result.Add(new VDFToken(VDFTokenType.StringStartMarker, i, result.Count, activeStringStartChar.ToString()));
							currentTokenTextBuilder.Remove(currentTokenTextBuilder.Length - 1, 1); // remove current char from the main-token text
							currentTokenFirstCharPos++; // don't count this inserted token text as part of main-token text

							// special case; if string-start-marker for an empty string
							if (ch == nextChar)
								result.Add(new VDFToken(VDFTokenType.String, currentTokenFirstCharPos, result.Count, ""));

							continue;
						}
					}
					else if (activeStringStartChar == ch)
					{
						//currentTokenType = VDFTokenType.StringEndMarker;
						if (parseAllTokens)
							result.Add(new VDFToken(VDFTokenType.StringEndMarker, i, result.Count, ch.ToString()));
						currentTokenTextBuilder.Remove(currentTokenTextBuilder.Length - 1, 1); // remove current char from the main-token text
						currentTokenFirstCharPos++; // don't count this inserted token text as part of main-token text

						activeStringStartChar = null;

						continue;
					}
				}
			}

			// if not in literal
			if (activeLiteralStartChars == null)
				// if in a string
				if (activeStringStartChar != null)
				{
					// if last-char of string
					if (activeStringStartChar == nextChar)
						currentTokenType = VDFTokenType.String;
				}
				else
				{
					var firstTokenChar = currentTokenTextBuilder.Length == 1;
					if (ch == '#' && nextChar == '#' && firstTokenChar) // if first char of in-line-comment
					{
						currentTokenTextBuilder = new StringBuilder(text.Substring(i, (text.IndexOf("\n", i + 1) != -1 ? text.IndexOf("\n", i + 1) : text.Length) - i));
						currentTokenType = VDFTokenType.InLineComment;
						i += currentTokenTextBuilder.Length - 1; // have next char processed by the one right after comment (i.e. the line-break char)
					}
					else if (currentTokenTextBuilder.ToString().TrimStart(options.allowCommaSeparators ? new[] {' ', ','} : new[] {' '}).Length == 0 && (ch == ' ' || (options.allowCommaSeparators && ch == ',')) && (nextChar != ' ' && (!options.allowCommaSeparators || nextChar != ','))) // if last char of space-or-comma-span
						currentTokenType = VDFTokenType.SpaceOrCommaSpan;

					else if (ch == '\t' && firstTokenChar)
						currentTokenType = VDFTokenType.Tab;
					else if (ch == '\n' && firstTokenChar)
						currentTokenType = VDFTokenType.LineBreak;

					//else if (nextNonSpaceChar == '>')
					else if (nextChar == '>')
						currentTokenType = VDFTokenType.Metadata;
					else if (ch == '>' && firstTokenChar)
						currentTokenType = VDFTokenType.MetadataEndMarker;
					//else if (nextNonSpaceChar == ':' && ch != ' ')
					else if (nextChar == ':' && ch != ' ')
						currentTokenType = VDFTokenType.Key;
					else if (ch == ':' && firstTokenChar)
						currentTokenType = VDFTokenType.KeyValueSeparator;
					else if (ch == '^' && firstTokenChar)
						currentTokenType = VDFTokenType.PoppedOutChildGroupMarker;

					else if (currentTokenTextBuilder.Length == 4 && currentTokenTextBuilder.ToString() == "null" && (!nextChar.HasValue || !charsAToZ.Contains(nextChar.Value))) // if text-so-far is 'null', and there's no more letters
						currentTokenType = VDFTokenType.Null;
					else if ((currentTokenTextBuilder.Length == 5 && currentTokenTextBuilder.ToString() == "false") || (currentTokenTextBuilder.Length == 4 && currentTokenTextBuilder.ToString() == "true"))
						currentTokenType = VDFTokenType.Boolean;
					else if
					(
						// if normal ("-12345" or "-123.45" or "-123e-45") number
						(
							chars0To9DotAndNegative.Contains(currentTokenTextBuilder[0]) && currentTokenTextBuilder[0].ToString().ToLower() != "e" // if first-char is valid as start of number
							&& chars0To9DotAndNegative.Contains(ch) // and current-char is valid as part of number
							&& (!nextChar.HasValue || !chars0To9DotAndNegative.Contains(nextChar.Value)) && nextChar != 'I' // and next-char is not valid as part of number
							&& (lastScopeIncreaseChar == "[" || result.Count == 0 || result.Last().type == VDFTokenType.Metadata || result.Last().type == VDFTokenType.KeyValueSeparator)
							)
						// or infinity number
						|| ((currentTokenTextBuilder.Length == 8 && currentTokenTextBuilder.ToString() == "Infinity") || (currentTokenTextBuilder.Length == 9 && currentTokenTextBuilder.ToString() == "-Infinity"))
					)
						currentTokenType = VDFTokenType.Number;
					else if (ch == '[' && firstTokenChar)
					{
						currentTokenType = VDFTokenType.ListStartMarker;
						lastScopeIncreaseChar = ch.ToString();
					}
					else if (ch == ']' && firstTokenChar)
						currentTokenType = VDFTokenType.ListEndMarker;
					else if (ch == '{' && firstTokenChar)
					{
						currentTokenType = VDFTokenType.MapStartMarker;
						lastScopeIncreaseChar = ch.ToString();
					}
					else if (ch == '}' && firstTokenChar)
						currentTokenType = VDFTokenType.MapEndMarker;
				}

				if (currentTokenType != VDFTokenType.None)
				{
					if (parseAllTokens || (currentTokenType != VDFTokenType.InLineComment && currentTokenType != VDFTokenType.SpaceOrCommaSpan && currentTokenType != VDFTokenType.MetadataEndMarker))
						result.Add(new VDFToken(currentTokenType, currentTokenFirstCharPos, result.Count, currentTokenTextBuilder.ToString()));

					currentTokenFirstCharPos = i + 1;
					currentTokenTextBuilder.Length = 0; // clear
					currentTokenType = VDFTokenType.None;
				}
			}

		if (postProcessTokens)
			//PostProcessTokens(result, options);
			result = PostProcessTokens(result, options);

		return result;
	}
	/*static int FindNextNonXCharPosition(string text, int startPos, char x)
	{
		for (int i = startPos; i < text.Length; i++)
			if (text[i] != x)
				return i;
		return -1;
	}*/
	/*static string UnpadString(string paddedString)
	{
		var result = paddedString;
		if (result.StartsWith("#"))
			result = result.Substring(1); // chop off first char, as it was just added by the serializer for separation
		if (result.EndsWith("#"))
			result = result.Substring(0, result.Length - 1);
		return result;
	}*/

	/*static List<VDFToken> PostProcessTokens(List<VDFToken> tokens, VDFLoadOptions options)
	{
		// pass 1: update strings-before-key-value-separator-tokens to be considered keys, if that's enabled (for JSON compatibility)
		// ----------

		if (options.allowStringKeys)
			for (var i = 0; i < tokens.Count; i++)
				if (tokens[i].type == VDFTokenType.String && i + 1 < tokens.Count && tokens[i + 1].type == VDFTokenType.KeyValueSeparator)
					tokens[i].type = VDFTokenType.Key;

		// pass 2: re-wrap popped-out-children with parent brackets/braces
		// ----------

		//var oldTokens = tokens.ToList();
		var result = new List<VDFToken>(); //tokens.ToList();

		tokens.Add(new VDFToken(VDFTokenType.None, -1, -1, "")); // maybe temp: add depth-0-ender helper token

		var fakeInsertPoint = -1;
		var fakeInsert = new List<VDFToken>();

		var line_tabsReached = 0;
		var tabDepth_popOutBlockEndWrapTokens = new Dictionary<int, List<VDFToken>>();
		for (var i = 0; i < tokens.Count + fakeInsert.Count; i++)
		{
			if (fakeInsertPoint != -1 && fakeInsert.Count < (i - fakeInsertPoint) + 1)
			{
				fakeInsertPoint = -1;
				i -= fakeInsert.Count;
			}

			VDFToken lastToken = fakeInsertPoint != -1 && fakeInsertPoint <= i - 1 ? fakeInsert[i - 1 - fakeInsertPoint] : (i - 1 >= 0 ? tokens[i - 1] : null);
			VDFToken token = fakeInsertPoint != -1 && fakeInsertPoint <= i ? fakeInsert[i - fakeInsertPoint] : tokens[i];
			if (token.type == VDFTokenType.Tab)
				line_tabsReached++;
			else if (token.type == VDFTokenType.LineBreak)
				line_tabsReached = 0;
			else if (token.type == VDFTokenType.PoppedOutChildGroupMarker)
			{
				if (lastToken.type == VDFTokenType.ListStartMarker || lastToken.type == VDFTokenType.MapStartMarker) //lastToken.type != VDFTokenType.Tab)
				{
					var enderTokenIndex = i + 1;
					while (enderTokenIndex < tokens.Count - 1 && tokens[enderTokenIndex].type != VDFTokenType.LineBreak && (tokens[enderTokenIndex].type != VDFTokenType.PoppedOutChildGroupMarker || tokens[enderTokenIndex - 1].type == VDFTokenType.ListStartMarker || tokens[enderTokenIndex - 1].type == VDFTokenType.MapStartMarker))
						enderTokenIndex++;
					var wrapGroupTabDepth = tokens[enderTokenIndex].type == VDFTokenType.PoppedOutChildGroupMarker ? line_tabsReached - 1 : line_tabsReached;
					tabDepth_popOutBlockEndWrapTokens[wrapGroupTabDepth] = tokens.GetRange(i + 1, enderTokenIndex - (i + 1));
					i = enderTokenIndex - 1; // have next token processed be the ender-token (^^[...] or line-break)
				}
				else if (lastToken.type == VDFTokenType.Tab)
				{
					var wrapGroupTabDepth = lastToken.type == VDFTokenType.Tab ? line_tabsReached - 1 : line_tabsReached;
					result.AddRange(tabDepth_popOutBlockEndWrapTokens[wrapGroupTabDepth]);
					/*foreach (VDFToken token2 in tabDepth_popOutBlockEndWrapTokens[wrapGroupTabDepth])
					{
						/*tokens.Remove(token2);
						tokens.Insert(i - 1, token2);*#/
						result.Add(token2);
					}*#/
					fakeInsertPoint = i;
					fakeInsert = tabDepth_popOutBlockEndWrapTokens[wrapGroupTabDepth];
					i--; // reprocess current pos (since it now has fake-insert data)

					//i -= tabDepth_popOutBlockEndWrapTokens[wrapGroupTabDepth].Count + 1; // have next token processed be the first pop-out-block-end-wrap-token
					tabDepth_popOutBlockEndWrapTokens.Remove(wrapGroupTabDepth);
				}
			}
			else if (lastToken != null && (lastToken.type == VDFTokenType.LineBreak || lastToken.type == VDFTokenType.Tab || token.type == VDFTokenType.None))
			{
				if (token.type == VDFTokenType.None) // if depth-0-ender helper token
					line_tabsReached = 0;
				if (tabDepth_popOutBlockEndWrapTokens.Count > 0)
					for (int tabDepth = tabDepth_popOutBlockEndWrapTokens.Max(a=>a.Key); tabDepth >= line_tabsReached; tabDepth--)
						if (tabDepth_popOutBlockEndWrapTokens.ContainsKey(tabDepth))
						{
							result.AddRange(tabDepth_popOutBlockEndWrapTokens[tabDepth]);
							/*foreach (VDFToken token2 in tabDepth_popOutBlockEndWrapTokens[tabDepth])
							{
								/*tokens.Remove(token2);
								tokens.Insert(i - 1, token2);*#/
								result.Add(token2);
							}*#/
							fakeInsertPoint = i;
							fakeInsert = tabDepth_popOutBlockEndWrapTokens[tabDepth];
							i--; // reprocess current pos (since it now has fake-insert data)

							tabDepth_popOutBlockEndWrapTokens.Remove(tabDepth);
						}
			}

			if (!tabDepth_popOutBlockEndWrapTokens.Values.Any(a=>a.Contains(token)) && !(token.type == VDFTokenType.Tab || token.type == VDFTokenType.LineBreak || token.type == VDFTokenType.MetadataEndMarker || token.type == VDFTokenType.KeyValueSeparator || token.type == VDFTokenType.PoppedOutChildGroupMarker))
				result.Add(token);
		}

		// maybe temp: remove depth-0-ender helper token
		tokens.RemoveAt(tokens.Count - 1);
		result.RemoveAt(result.Count - 1);

		// pass 3: remove all now-useless tokens
		// ----------

		/*for (var i = tokens.Count - 1; i >= 0; i--)
			if (tokens[i].type == VDFTokenType.Tab || tokens[i].type == VDFTokenType.LineBreak || tokens[i].type == VDFTokenType.MetadataEndMarker || tokens[i].type == VDFTokenType.KeyValueSeparator || tokens[i].type == VDFTokenType.PoppedOutChildGroupMarker)
				tokens.RemoveAt(i);*#/

		/*foreach(VDFToken token in oldTokens)
			if (!(token.type == VDFTokenType.Tab || token.type == VDFTokenType.LineBreak || token.type == VDFTokenType.MetadataEndMarker || token.type == VDFTokenType.KeyValueSeparator || token.type == VDFTokenType.PoppedOutChildGroupMarker))
				tokens.Add(token);*#/

		// pass 4: fix token position-and-index properties
		// ----------

		RefreshTokenPositionAndIndexProperties(result); //tokens);

		//Console.Write(String.Join(" ", tokens.Select(a=>a.text).ToArray())); // temp; for testing

		return result;
	}*/
	static List<VDFToken> PostProcessTokens(List<VDFToken> tokens, VDFLoadOptions options)
	{
		// pass 1: update strings-before-key-value-separator-tokens to be considered keys, if that's enabled (one reason being, for JSON compatibility)
		// ----------

		if (options.allowStringKeys)
			for (var i = 0; i < tokens.Count; i++)
				if (tokens[i].type == VDFTokenType.String && i + 1 < tokens.Count && tokens[i + 1].type == VDFTokenType.KeyValueSeparator)
					tokens[i].type = VDFTokenType.Key;

		// pass 2: re-wrap popped-out-children with parent brackets/braces
		// ----------

		tokens.Add(new VDFToken(VDFTokenType.None, -1, -1, "")); // maybe temp: add depth-0-ender helper token

		var line_tabsReached = 0;
		var tabDepth_popOutBlockEndWrapTokens = new Dictionary<int, List<VDFToken>>();
		for (var i = 0; i < tokens.Count; i++)
		{
			VDFToken lastToken = i - 1 >= 0 ? tokens[i - 1] : null;
			VDFToken token = tokens[i];
			if (token.type == VDFTokenType.Tab)
				line_tabsReached++;
			else if (token.type == VDFTokenType.LineBreak)
				line_tabsReached = 0;
			else if (token.type == VDFTokenType.PoppedOutChildGroupMarker)
			{
				if (lastToken.type == VDFTokenType.ListStartMarker || lastToken.type == VDFTokenType.MapStartMarker) //lastToken.type != VDFTokenType.Tab)
				{
					var enderTokenIndex = i + 1;
					while (enderTokenIndex < tokens.Count - 1 && tokens[enderTokenIndex].type != VDFTokenType.LineBreak && (tokens[enderTokenIndex].type != VDFTokenType.PoppedOutChildGroupMarker || tokens[enderTokenIndex - 1].type == VDFTokenType.ListStartMarker || tokens[enderTokenIndex - 1].type == VDFTokenType.MapStartMarker))
						enderTokenIndex++;
					var wrapGroupTabDepth = tokens[enderTokenIndex].type == VDFTokenType.PoppedOutChildGroupMarker ? line_tabsReached - 1 : line_tabsReached;
					tabDepth_popOutBlockEndWrapTokens[wrapGroupTabDepth] = tokens.GetRange(i + 1, enderTokenIndex - (i + 1));
					tabDepth_popOutBlockEndWrapTokens[wrapGroupTabDepth][0].index = i + 1; // update index
					i = enderTokenIndex - 1; // have next token processed be the ender-token (^^[...] or line-break)
				}
				else if (lastToken.type == VDFTokenType.Tab)
				{
					var wrapGroupTabDepth = lastToken.type == VDFTokenType.Tab ? line_tabsReached - 1 : line_tabsReached;
					tokens.InsertRange(i, tabDepth_popOutBlockEndWrapTokens[wrapGroupTabDepth]);
					//tokens.RemoveRange(tokens.IndexOf(tabDepth_popOutBlockEndWrapTokens[wrapGroupTabDepth][0]), tabDepth_popOutBlockEndWrapTokens[wrapGroupTabDepth].Count);
					tokens.RemoveRange(tabDepth_popOutBlockEndWrapTokens[wrapGroupTabDepth][0].index, tabDepth_popOutBlockEndWrapTokens[wrapGroupTabDepth].Count); // index was updated when set put together
					/*foreach (VDFToken token2 in tabDepth_popOutBlockEndWrapTokens[wrapGroupTabDepth])
					{
						tokens.Remove(token2);
						tokens.Insert(i - 1, token2);
					}*/

					// maybe temp; fix for that tokens were not post-processed correctly for multiply-nested popped-out maps/lists
					RefreshTokenPositionAndIndexProperties(tokens);

					i -= tabDepth_popOutBlockEndWrapTokens[wrapGroupTabDepth].Count + 1; // have next token processed be the first pop-out-block-end-wrap-token
					tabDepth_popOutBlockEndWrapTokens.Remove(wrapGroupTabDepth);
				}
			}
			else if (lastToken != null && (lastToken.type == VDFTokenType.LineBreak || lastToken.type == VDFTokenType.Tab || token.type == VDFTokenType.None))
			{
				if (token.type == VDFTokenType.None) // if depth-0-ender helper token
					line_tabsReached = 0;
				if (tabDepth_popOutBlockEndWrapTokens.Count > 0)
					for (int tabDepth = tabDepth_popOutBlockEndWrapTokens.Max(a=>a.Key); tabDepth >= line_tabsReached; tabDepth--)
						if (tabDepth_popOutBlockEndWrapTokens.ContainsKey(tabDepth))
						{
							tokens.InsertRange(i, tabDepth_popOutBlockEndWrapTokens[tabDepth]);
							//tokens.RemoveRange(tokens.IndexOf(tabDepth_popOutBlockEndWrapTokens[tabDepth][0]), tabDepth_popOutBlockEndWrapTokens[tabDepth].Count);
							tokens.RemoveRange(tabDepth_popOutBlockEndWrapTokens[tabDepth][0].index, tabDepth_popOutBlockEndWrapTokens[tabDepth].Count); // index was updated when set put together
							/*foreach (VDFToken token2 in tabDepth_popOutBlockEndWrapTokens[tabDepth])
							{
								tokens.Remove(token2);
								tokens.Insert(i - 1, token2);
							}*/

							// maybe temp; fix for that tokens were not post-processed correctly for multiply-nested popped-out maps/lists
							RefreshTokenPositionAndIndexProperties(tokens);

							tabDepth_popOutBlockEndWrapTokens.Remove(tabDepth);
						}
			}
		}

		tokens.RemoveAt(tokens.Count - 1); // maybe temp: remove depth-0-ender helper token

		// pass 3: remove all now-useless tokens
		// ----------

		/*for (var i = tokens.Count - 1; i >= 0; i--)
			if (tokens[i].type == VDFTokenType.Tab || tokens[i].type == VDFTokenType.LineBreak || tokens[i].type == VDFTokenType.MetadataEndMarker || tokens[i].type == VDFTokenType.KeyValueSeparator || tokens[i].type == VDFTokenType.PoppedOutChildGroupMarker)
				tokens.RemoveAt(i);*/

		var result = new List<VDFToken>();
		foreach (VDFToken token in tokens)
			if (!(token.type == VDFTokenType.Tab || token.type == VDFTokenType.LineBreak || token.type == VDFTokenType.MetadataEndMarker || token.type == VDFTokenType.KeyValueSeparator || token.type == VDFTokenType.PoppedOutChildGroupMarker))
				result.Add(token);

		// pass 4: fix token position-and-index properties
		// ----------

		RefreshTokenPositionAndIndexProperties(result); //tokens);

		// temp; for testing
		/*Console.WriteLine(String.Join(" ", tokens.Select(a=>a.text).ToArray()));
		Console.WriteLine("==========");
		Console.WriteLine(String.Join(" ", result.Select(a=>a.text).ToArray()));*/

		return result;
	}
	static void RefreshTokenPositionAndIndexProperties(List<VDFToken> tokens)
	{
		var textProcessedLength = 0;
		for (var i = 0; i < tokens.Count; i++)
		{
			var token = tokens[i];
			token.position = textProcessedLength;
			token.index = i;
			textProcessedLength += token.text.Length;
		}
	}
}