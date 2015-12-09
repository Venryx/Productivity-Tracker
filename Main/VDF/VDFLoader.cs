using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class VDFLoadOptions
{
	public VDFLoadOptions(List<object> message = null, bool allowStringKeys = true, bool allowCommaSeparators = false, Dictionary<string, string> namespaceAliasesByName = null, Dictionary<Type, string> typeAliasesByType = null)
	{
		this.messages = message ?? new List<object>();
		this.allowStringKeys = allowStringKeys;
		this.allowCommaSeparators = allowCommaSeparators;
		this.namespaceAliasesByName = namespaceAliasesByName ?? new Dictionary<string, string>();
		this.typeAliasesByType = typeAliasesByType ?? new Dictionary<Type, string>();
	}

	public List<object> messages;
	public Dictionary<object, List<Action>> objPostDeserializeFuncs_early = new Dictionary<object, List<Action>>();
	public Dictionary<object, List<Action>> objPostDeserializeFuncs = new Dictionary<object, List<Action>>();
	public void AddObjPostDeserializeFunc(object obj, Action func, bool early = false)
	{
		if (early)
		{
			if (!objPostDeserializeFuncs_early.ContainsKey(obj))
				objPostDeserializeFuncs_early.Add(obj, new List<Action>());
			objPostDeserializeFuncs_early[obj].Add(func);
		}
		else
		{
			if (!objPostDeserializeFuncs.ContainsKey(obj))
				objPostDeserializeFuncs.Add(obj, new List<Action>());
			objPostDeserializeFuncs[obj].Add(func);
		}
	}

	// for JSON compatibility
	public bool allowStringKeys;
	public bool allowCommaSeparators;

	// CS only
	public Dictionary<string, string> namespaceAliasesByName;
	public Dictionary<Type, string> typeAliasesByType;
	//public List<string> extraSearchAssemblyNames; // maybe add this option later

	public VDFLoadOptions ForJSON() // helper function for JSON compatibility
	{
		allowStringKeys = true;
		allowCommaSeparators = true;
		return this;
	}
}

public static class VDFLoader
{
	public static VDFNode ToVDFNode<T>(string text, VDFLoadOptions options = null) { return ToVDFNode(text, typeof(T), options); }
	public static VDFNode ToVDFNode(string text, VDFLoadOptions options) { return ToVDFNode(text, null, options); }
	public static VDFNode ToVDFNode(string text, Type declaredType = null, VDFLoadOptions options = null) { return ToVDFNode(VDFTokenParser.ParseTokens(text, options), declaredType, options); }
	public static VDFNode ToVDFNode(List<VDFToken> tokens, Type declaredType = null, VDFLoadOptions options = null, int firstTokenIndex = 0, int enderTokenIndex = -1)
	{
		options = options ?? new VDFLoadOptions();
		enderTokenIndex = enderTokenIndex != -1 ? enderTokenIndex : tokens.Count;

		// figure out obj-type
		// ==========

		var depth = 0;
		var tokensAtDepth0 = new List<VDFToken>();
		var tokensAtDepth1 = new List<VDFToken>();
		//foreach (VDFToken token in tokens)
		for (var i = firstTokenIndex; i < enderTokenIndex; i++)
		{
			var token = tokens[i];
			if (token.type == VDFTokenType.ListEndMarker || token.type == VDFTokenType.MapEndMarker)
				depth--;
			if (depth == 0)
				tokensAtDepth0.Add(token);
			if (depth == 1)
				tokensAtDepth1.Add(token);
			if (token.type == VDFTokenType.ListStartMarker || token.type == VDFTokenType.MapStartMarker)
				depth++;
		}

		var fromVDFTypeName = "object";
		var firstNonMetadataToken = tokensAtDepth0.First(a=>a.type != VDFTokenType.Metadata);
		if (tokensAtDepth0[0].type == VDFTokenType.Metadata)
			fromVDFTypeName = tokensAtDepth0[0].text;
		else if (firstNonMetadataToken.type == VDFTokenType.Boolean)
			fromVDFTypeName = "bool";
		else if (firstNonMetadataToken.type == VDFTokenType.Number)
			fromVDFTypeName = firstNonMetadataToken.text.Contains(".") ? "double" : "int";
		else if (firstNonMetadataToken.type == VDFTokenType.String)
			fromVDFTypeName = "string";
		else if (firstNonMetadataToken.type == VDFTokenType.ListStartMarker)
			fromVDFTypeName = "List(object)";
		else if (firstNonMetadataToken.type == VDFTokenType.MapStartMarker)
			fromVDFTypeName = "Dictionary(object object)"; //"object";

		Type type = declaredType;
		if (fromVDFTypeName != null && fromVDFTypeName.Length > 0)
		{
			var fromVDFType = VDF.GetTypeByName(fromVDFTypeName, options);
			if (type == null || type.IsAssignableFrom(fromVDFType)) // if there is no declared type, or the from-vdf type is more specific than the declared type
				type = fromVDFType;
		}
		var typeGenericArgs = VDF.GetGenericArgumentsOfType(type);
		var typeInfo = VDFTypeInfo.Get(type);

		// create the object's VDFNode, and load in the data
		// ==========

		var node = new VDFNode();
		node.metadata = tokensAtDepth0[0].type == VDFTokenType.Metadata ? fromVDFTypeName : null;
		
		// if primitive, parse value
		if (firstNonMetadataToken.type == VDFTokenType.Null)
			node.primitiveValue = null;
		else if (firstNonMetadataToken.type == VDFTokenType.Boolean)
			node.primitiveValue = bool.Parse(firstNonMetadataToken.text);
		else if (firstNonMetadataToken.type == VDFTokenType.Number)
			if (firstNonMetadataToken.text == "Infinity")
				node.primitiveValue = double.PositiveInfinity;
			else if (firstNonMetadataToken.text == "-Infinity")
				node.primitiveValue = double.NegativeInfinity;
			else if (firstNonMetadataToken.text.Contains(".") || firstNonMetadataToken.text.Contains("e"))
				node.primitiveValue = double.Parse(firstNonMetadataToken.text);
			else
				node.primitiveValue = int.Parse(firstNonMetadataToken.text);
		else if (firstNonMetadataToken.type == VDFTokenType.String)
			node.primitiveValue = firstNonMetadataToken.text;

		// if list, parse items
		else if (typeof(IList).IsAssignableFrom(type))
		{
			node.isList = true;
			for (var i = 0; i < tokensAtDepth1.Count; i++)
			{
				var token = tokensAtDepth1[i];
				if (token.type != VDFTokenType.ListEndMarker && token.type != VDFTokenType.MapEndMarker)
				{
					var itemFirstToken = tokens[token.index];
					var itemEnderToken = tokensAtDepth1.FirstOrDefault(a=>a.index > itemFirstToken.index + (itemFirstToken.type == VDFTokenType.Metadata ? 1 : 0) && token.type != VDFTokenType.ListEndMarker && token.type != VDFTokenType.MapEndMarker);
					//node.listChildren.Add(ToVDFNode(GetTokenRange_Tokens(tokens, itemFirstToken, itemEnderToken), typeGenericArgs[0], options));
					node.listChildren.Add(ToVDFNode(tokens, typeGenericArgs[0], options, itemFirstToken.index, itemEnderToken != null ? itemEnderToken.index : enderTokenIndex));
					if (itemFirstToken.type == VDFTokenType.Metadata) // if item had metadata, skip an extra token (since it had two non-end tokens)
						i++;
				}
			}
		}

		// if not primitive and not list (i.e. map/object/dictionary), parse pairs/properties
		else //if (!typeof(IList).IsAssignableFrom(objType))
		{
			node.isMap = true;
			for (var i = 0; i < tokensAtDepth1.Count; i++)
			{
				var token = tokensAtDepth1[i];
				if (token.type == VDFTokenType.Key)
				{
					var propName = token.text;
					Type propValueType;
					if (typeof(IDictionary).IsAssignableFrom(type))
						propValueType = typeGenericArgs[1];
					else
						propValueType = typeInfo.props.ContainsKey(propName) ? typeInfo.props[propName].GetPropType() : null;

					var propValueFirstToken = tokensAtDepth1[i + 1];
					var propValueEnderToken = tokensAtDepth1.FirstOrDefault(a=>a.index > propValueFirstToken.index && a.type == VDFTokenType.Key);
					//node.mapChildren.Add(propName, ToVDFNode(GetTokenRange_Tokens(tokens, propValueFirstToken, propValueEnderToken), propValueType, options));
					node.mapChildren.Add(propName, ToVDFNode(tokens, propValueType, options, propValueFirstToken.index, propValueEnderToken != null ? propValueEnderToken.index : enderTokenIndex));
				}
			}
		}

		return node;
	}
	/*static List<VDFToken> GetTokenRange_Tokens(List<VDFToken> tokens, VDFToken firstToken, VDFToken enderToken)
	{
		//return tokens.GetRange(firstToken.index, (enderToken != null ? enderToken.index : tokens.Count) - firstToken.index).Select(a=>new VDFToken(a.type, a.position - firstToken.position, a.index - firstToken.index, a.text)).ToList();

		var result = new List<VDFToken>(); //(enderToken != null ? enderToken.index : tokens.Count) - firstToken.index);
		for (var i = firstToken.index; i < (enderToken != null ? enderToken.index : tokens.Count); i++)
			result.Add(new VDFToken(tokens[i].type, tokens[i].position - firstToken.position, tokens[i].index - firstToken.index, tokens[i].text));
		return result;
	}*/
}