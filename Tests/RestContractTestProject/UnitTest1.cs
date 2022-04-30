using DtoKit.Demo;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Net.Leksi.RestContract;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace RestContractTestProject
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            CodeGenerator cg = new();
            Dictionary<string, string> map = new();
            cg.GenerateHelpers<IConnector>("DtoKit.Demo.IDemoController", "DtoKit.Demo.DemoControllerProxy", "DtoKit.Demo.DemoConnectorBase", map);
            Console.WriteLine(string.Join("\n//--------------\n// Cut here\n//--------------\n", map.Values));

        }

    }
}