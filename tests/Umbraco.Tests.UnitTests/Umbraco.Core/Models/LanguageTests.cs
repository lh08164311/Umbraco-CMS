// Copyright (c) Umbraco.
// See LICENSE for more details.

using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace Umbraco.Cms.Tests.UnitTests.Umbraco.Core.Models
{
    [TestFixture]
    public class LanguageTests
    {
        private LanguageBuilder _builder = new LanguageBuilder();

        [SetUp]
        public void SetUp() => _builder = new LanguageBuilder();

        [Test]
        public void Can_Deep_Clone()
        {
            ILanguage item = _builder
                .WithId(1)
                .Build();

            var clone = (Language)item.DeepClone();
            Assert.AreNotSame(clone, item);
            Assert.AreEqual(clone, item);
            Assert.AreEqual(clone.CreateDate, item.CreateDate);
            Assert.AreEqual(clone.CultureName, item.CultureName);
            Assert.AreEqual(clone.Id, item.Id);
            Assert.AreEqual(clone.IsoCode, item.IsoCode);
            Assert.AreEqual(clone.Key, item.Key);
            Assert.AreEqual(clone.UpdateDate, item.UpdateDate);

            // This double verifies by reflection
            PropertyInfo[] allProps = clone.GetType().GetProperties();
            foreach (PropertyInfo propertyInfo in allProps)
            {
                Assert.AreEqual(propertyInfo.GetValue(clone, null), propertyInfo.GetValue(item, null));
            }
        }

        [Test]
        public void Can_Serialize_Without_Error()
        {
            ILanguage item = _builder.Build();

            Assert.DoesNotThrow(() => JsonConvert.SerializeObject(item));
        }
    }
}
