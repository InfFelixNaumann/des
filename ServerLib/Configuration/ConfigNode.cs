﻿#region -- copyright --
//
// Licensed under the EUPL, Version 1.1 or - as soon they will be approved by the
// European Commission - subsequent versions of the EUPL(the "Licence"); You may
// not use this work except in compliance with the Licence.
//
// You may obtain a copy of the Licence at:
// http://ec.europa.eu/idabc/eupl
//
// Unless required by applicable law or agreed to in writing, software distributed
// under the Licence is distributed on an "AS IS" basis, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, either express or implied. See the Licence for the
// specific language governing permissions and limitations under the Licence.
//
#endregion
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Xml.Linq;
using Neo.IronLua;
using TecWare.DE.Stuff;

namespace TecWare.DE.Server.Configuration
{
	/// <summary>Configuration node, that read attributes and elements with support from the schema.</summary>
	public sealed class XConfigNode : DynamicObject, IPropertyEnumerableDictionary
	{
		#region -- class XConfigNodes -------------------------------------------------

		private sealed class XConfigNodes : DynamicObject, IReadOnlyList<XConfigNode>
		{
			private readonly XConfigNode[] elements;
			private readonly IDEConfigurationAttribute primaryKey;
			private readonly IDEConfigurationElement configurationElement;

			public XConfigNodes(XElement parentElement, IDEConfigurationElement configurationElement)
			{
				if (parentElement == null)
					throw new ArgumentNullException(nameof(parentElement));

				this.configurationElement = configurationElement ?? throw new ArgumentNullException(nameof(configurationElement));
				elements = parentElement.Elements(configurationElement.Name).Select(x => new XConfigNode(configurationElement, x)).ToArray();
				primaryKey = configurationElement.GetAttributes().FirstOrDefault(a => a.IsPrimaryKey);
			} // ctor

			public IEnumerator<XConfigNode> GetEnumerator()
				=> elements.Cast<XConfigNode>().GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator()
				=> elements.GetEnumerator();

			private string GetPrimaryKeyValue(XConfigNode c)
			{
				var value = GetConfigurationValue(primaryKey, c.GetAttributeValueCore(primaryKey));
				return value?.ChangeType<string>();
			}  // func GetPrimaryKeyValue

			private IEnumerable<string> GetCurrentMembers()
			{
				foreach (var c in elements)
				{
					var value = GetPrimaryKeyValue(c);
					if (value != null)
						yield return value;
				}
			} // func  GetCurrentMembers

			public override IEnumerable<string> GetDynamicMemberNames()
				=> primaryKey == null ? Array.Empty<string>() : GetCurrentMembers();

			private bool TryFindMember(string memberName, out XConfigNode result)
			{
				result = elements.FirstOrDefault(x => String.Compare(memberName, GetPrimaryKeyValue(x), StringComparison.OrdinalIgnoreCase) == 0);
				return result != null;
			} // func TryFindMember

			public override bool TryGetMember(GetMemberBinder binder, out object result)
			{
				if (primaryKey == null)
					return base.TryGetMember(binder, out result);
				else if (TryFindMember(binder.Name, out var configNode))
				{
					result = configNode;
					return true;
				}
				else
					return base.TryGetMember(binder, out result);
			} // func TryGetMember

			public int Count => elements.Length;
			public XConfigNode this[int index] => index >= 0 && index < elements.Length ? elements[index] : null;
			public XConfigNode this[string member] => primaryKey != null && TryFindMember(member, out var res) ? res : null;
		} // class XConfigNodes

		#endregion

		private readonly IDEConfigurationElement configurationElement;
		private readonly Lazy<Dictionary<string, IDEConfigurationAnnotated>> getElements;
		private readonly XElement element;

		private XConfigNode(IDEConfigurationElement configurationElement, XElement element)
		{
			this.configurationElement = configurationElement ?? throw new ArgumentNullException(nameof(configurationElement));

			getElements = new Lazy<Dictionary<string, IDEConfigurationAnnotated>>(() =>
				{
					var r = new Dictionary<string, IDEConfigurationAnnotated>(StringComparer.OrdinalIgnoreCase);

					// value of the element
					if (configurationElement.Value != null)
						r[String.Empty] = null;

					// sub attributes
					foreach (var attr in configurationElement.GetAttributes())
						r[attr.Name.LocalName] = attr;

					// sub elements
					foreach (var el in configurationElement.GetElements())
						r[el.Name.LocalName] = el;
					return r;
				}
			);

			this.element = element;
		} // ctor

		private static object GetConfigurationValueSingle(IDEConfigurationValue attr, string value)
		{
			var type = attr.Type;
			if (type == typeof(LuaType))
			{
				if (value == null)
					value = attr.DefaultValue ?? "object";
				return LuaType.GetType(value, false, false).Type;
			}
			else if (type == typeof(Encoding))
			{
				if (String.IsNullOrEmpty(value))
					value = attr.DefaultValue;

				if (String.IsNullOrEmpty(value))
					return Encoding.Default;
				else if (Int32.TryParse(value, out var codePage))
					return Encoding.GetEncoding(codePage);
				else
					return Encoding.GetEncoding(value);
			}
			else if (type == typeof(CultureInfo))
			{
				return String.IsNullOrEmpty(value)
					? CultureInfo.GetCultureInfo(attr.DefaultValue)
					: CultureInfo.GetCultureInfo(value);
			}
			else if (type == typeof(DirectoryInfo))
			{
				return String.IsNullOrEmpty(value)
					? null
					: new DirectoryInfo(value);
			}
			else if (type == typeof(SecureString))
			{
				try
				{
					return Passwords.DecodePassword(value);
				}
				catch
				{
					return null;
				}
			}
			else if (type == typeof(FileSize))
			{
				return FileSize.TryParse(value ?? attr.DefaultValue, out var fileSize)
					? fileSize
					: FileSize.Empty;
			}
			else
			{
				try
				{
					return Procs.ChangeType(value ?? attr.DefaultValue, type);
				}
				catch
				{
					return Procs.ChangeType(attr.DefaultValue, type);
				}
			}
		} // func GetConfigurationValue

		internal static object GetConfigurationValue(IDEConfigurationValue attr, string value)
		{
			var type = attr.Type;
			if (attr.IsList)
				return Procs.GetStrings(value).Select(v => GetConfigurationValueSingle(attr, v)).ToArray();
			else
				return GetConfigurationValueSingle(attr, value);
		} // func GetAttributeValue

		internal string GetAttributeValueCore(IDEConfigurationAttribute attr)
		{
			var value = attr.IsElement
				? element?.Element(attr.Name)?.Value
				: element?.Attribute(attr.Name)?.Value;

			return value;
		} // func GetAttributeValueCore

		private PropertyValue GetPropertyValue(IDEConfigurationAnnotated item)
		{
			switch (item)
			{
				case null:
					return new PropertyValue("(Default)", configurationElement.Value.Type, GetConfigurationValue(configurationElement.Value, element?.Value));
				case IDEConfigurationAttribute attr:
					return new PropertyValue(attr.Name.LocalName, attr.IsList ? attr.Type.MakeArrayType() : attr.Type, GetConfigurationValue(attr, GetAttributeValueCore(attr)));
				case IDEConfigurationElement el:
					if (el.MinOccurs == 1 && el.MaxOccurs == 1)
						return new PropertyValue(el.Name.LocalName, typeof(XConfigNode), new XConfigNode(el, element?.Element(el.Name)));
					else
						return new PropertyValue(el.Name.LocalName, typeof(IEnumerable<XConfigNode>), element != null ? new XConfigNodes(element, el) : null);
				default:
					return null;
			}
		} // func GetPropertyValue

		/// <summary>Get a attribute value or default value.</summary>
		/// <param name="name">Name of the attribute.</param>
		/// <param name="value">Value of the property.</param>
		/// <returns><c>true</c>, if the property is defined and has a value.</returns>
		public bool TryGetProperty(string name, out object value)
		{
			if (name != null && getElements.Value.TryGetValue(name, out var item))
			{
				var prop = GetPropertyValue(item);
				if (prop != null)
				{
					value = prop.Value;
					return true;
				}
				else
				{
					value = null;
					return false;
				}
			}
			else
			{
				value = null;
				return false;
			}
		} // func TryGetProperty

		/// <summary>Returns all attributes as properties.</summary>
		/// <returns></returns>
		public IEnumerator<PropertyValue> GetEnumerator()
		{
			foreach (var attr in getElements.Value)
				yield return GetPropertyValue(attr.Value);
		} // func GetEnumerator

		IEnumerator IEnumerable.GetEnumerator()
			=> GetEnumerator();

		/// <summary></summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name"></param>
		/// <returns></returns>
		public T GetAttribute<T>(string name)
			=> Procs.ChangeType<T>(GetAttribute(name));

		/// <summary></summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public object GetAttribute(string name)
			=> TryGetProperty(name, out var value)
				? value
				: throw new ArgumentException(String.Format("@{0} is not defined.", name));

		/// <summary></summary>
		/// <param name="binder"></param>
		/// <param name="indexes"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
		{
			if (binder.CallInfo.ArgumentCount == 1)
			{
				switch (indexes[0])
				{
					case string memberName:
						return TryGetProperty(memberName, out result);
					default:
						return base.TryGetIndex(binder, indexes, out result);
				}
			}
			else
				return base.TryGetIndex(binder, indexes, out result);
		} // func TryGetIndex

		/// <summary></summary>
		/// <param name="binder"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		public override bool TryGetMember(GetMemberBinder binder, out object result)
			=> TryGetProperty(binder.Name, out result) || base.TryGetMember(binder, out result);

		/// <summary>Return keys.</summary>
		/// <returns></returns>
		public override IEnumerable<string> GetDynamicMemberNames()
			=> getElements.Value.Keys;

		/// <summary></summary>
		public XName Name => element?.Name ?? configurationElement.Name;
		/// <summary></summary>
		public XElement Element => element;
		/// <summary>Value of the configuration element.</summary>
		public object Value => configurationElement.Value != null ? GetConfigurationValue(configurationElement.Value, element?.Value) : null;
		/// <summary></summary>
		public IDEConfigurationElement ConfigurationElement => configurationElement;

		// -- Static ----------------------------------------------------------

		private static IDEConfigurationElement GetConfigurationElement(IDEConfigurationService configurationService, XName name)
		{
			if (configurationService == null)
				throw new ArgumentNullException(nameof(configurationService));

			var configurationElement = configurationService[name ?? throw new ArgumentNullException(nameof(name))];
			return configurationElement ?? throw new ArgumentNullException($"Configuration definition not found for element '{name}'.");
		} // proc CheckConfigurationElement

		/// <summary>Create XConfigNode reader.</summary>
		/// <param name="configurationElement"></param>
		/// <param name="element"></param>
		/// <returns></returns>
		public static XConfigNode Create(IDEConfigurationElement configurationElement, XElement element)
		{
			if (configurationElement == null)
				throw new ArgumentNullException(nameof(configurationElement), $"Configuration definition not found for element '{element?.Name ?? "<null>"}'.");
			if (element != null && !configurationElement.IsName(element.Name))
				throw new ArgumentOutOfRangeException(nameof(element), $"Element '{configurationElement.Name}' does not match with '{element.Name}'.");

			return new XConfigNode(configurationElement, element);
		} // func Create

		/// <summary></summary>
		/// <param name="configurationService"></param>
		/// <param name="element"></param>
		/// <returns></returns>
		public static XConfigNode Create(IDEConfigurationService configurationService, XElement element)
		{
			if (element == null)
				throw new ArgumentNullException(nameof(element));

			return Create(GetConfigurationElement(configurationService, element.Name), element);
		} // func Create

		/// <summary></summary>
		/// <param name="configurationService"></param>
		/// <param name="baseElement"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public static XConfigNode GetElement(IDEConfigurationService configurationService, XElement baseElement, XName name)
			=> new XConfigNode(
				GetConfigurationElement(configurationService, name),
				baseElement?.Element(name)
			);

		/// <summary></summary>
		/// <param name="configurationService"></param>
		/// <param name="baseElement"></param>
		/// <returns></returns>
		public static IEnumerable<XConfigNode> GetElements(IDEConfigurationService configurationService, XElement baseElement)
		{
			IDEConfigurationElement lastConfigurationElement = null;
			foreach (var cur in baseElement.Elements())
			{
				if (lastConfigurationElement == null
					|| !lastConfigurationElement.IsName(cur.Name))
				{
					var tmp = configurationService[cur.Name];
					if (tmp == null)
						break;
					lastConfigurationElement = tmp;
				}

				yield return new XConfigNode(lastConfigurationElement, cur);
			}
		} // func GetElements

		/// <summary></summary>
		/// <param name="configurationService"></param>
		/// <param name="baseElement"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public static IEnumerable<XConfigNode> GetElements(IDEConfigurationService configurationService, XElement baseElement, XName name)
		{
			var configurationElement = GetConfigurationElement(configurationService, name);
			if (baseElement != null)
			{
				foreach (var cur in baseElement.Elements(name))
					yield return new XConfigNode(configurationElement, cur);
			}
		} // func GetElements
	} // class XConfigNode
}
