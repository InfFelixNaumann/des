﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TecWare.DE.Server.Configuration;

namespace TecWare.DE.Server
{
	[TestClass]
	public class ConfigurationTests
	{
		[TestMethod]
		public void LoadAssemblies()
		{
			var cs = new DEConfigurationService(new SimpleServiceProvider(), @"..\..\Files\LoadAssemblies.xml");
			cs.UpdateSchema(Assembly.LoadFile(Path.GetFullPath(@"..\..\..\Server\bin\Debug\DEServer.exe")));

			var n = cs[DEConfigurationConstants.MainNamespace + "configLogItem"];
			Assert.IsNotNull(n);
			Assert.AreEqual("configLogItem", n.Name.LocalName);
			Assert.AreEqual(typeof(DEConfigLogItem), n.ClassType);

			foreach (var c in n.GetAttributes())
				Console.WriteLine($"Attribute[{c.IsPrimaryKey}]: {c.Name.LocalName} : {c.TypeName} [{c.Type}]");

			var a2 = n.GetAttributes().FirstOrDefault(c => c.Name == "script");
			Assert.IsNotNull(a2);
			Assert.IsTrue(a2.IsList);

			foreach (var c in n.GetElements())
				Console.WriteLine($"Element: {c.Name.LocalName}");

			var n2 = n.GetElements().FirstOrDefault(c => c.Name == DEConfigurationConstants.MainNamespace + "log");
      Assert.IsNotNull(n2);
			Assert.IsNull(n2.ClassType);
			Assert.IsNotNull(n2.Documentation);

			var a1 = n2.GetAttributes().FirstOrDefault(c => c.Name == "min");
			Assert.IsNotNull(a1);
			Assert.AreEqual(typeof(uint), a1.Type);
			Assert.AreEqual((uint)3670016, a1.DefaultValue);
    }

		[TestMethod]
		public void MergeConfigurations()
		{
			var cs = new DEConfigurationService(new SimpleServiceProvider(), @"..\..\Files\01_Main.xml");
			cs.UpdateSchema(Assembly.LoadFile(Path.GetFullPath(@"..\..\..\Server\bin\Debug\DEServer.exe")));

			var x = cs.ParseConfiguration(new DE.Stuff.PropertyDictionary());

			Console.WriteLine(x.ToString());

			// tests
			var c1 = x.Elements(DEConfigurationConstants.MainNamespace + "configLogItem").First();
			Assert.IsNotNull(c1);
			Assert.AreEqual("test 1", c1.Attribute("displayname")?.Value);
			Assert.AreEqual("neu", c1.Attribute("icon")?.Value);

			var l1 = c1.Element(DEConfigurationConstants.xnLog);
			Assert.IsNotNull(l1);
			Assert.AreEqual("4096", l1.Attribute("min")?.Value);
			Assert.AreEqual("8128", l1.Attribute("max")?.Value);

			var c2 = x.Elements(DEConfigurationConstants.MainNamespace + "configLogItem").Skip(1).First();
			Assert.IsNotNull(c2);
			Assert.AreEqual("test 2", c2.Attribute("displayname")?.Value);
			Assert.AreEqual("script1 script2 script3", c2.Attribute("script")?.Value);
      var l2 = c2.Element(DEConfigurationConstants.xnLog);
			Assert.IsNotNull(l2);
			Assert.AreEqual("4096", l2.Attribute("min")?.Value);
			Assert.AreEqual("8128", l2.Attribute("max")?.Value);
		}
	} // class ConfigurationTest
}
