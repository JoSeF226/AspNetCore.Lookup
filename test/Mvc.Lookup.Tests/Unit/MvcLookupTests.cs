﻿using NSubstitute;
using System.Reflection;
using Xunit;

namespace NonFactors.Mvc.Lookup.Tests.Unit
{
    public class MvcLookupTests
    {
        #region Constants

        [Fact]
        public void Prefix_IsConstant()
        {
            Assert.True(typeof(MvcLookup).GetField("Prefix").IsLiteral);
            Assert.Equal("Lookup", MvcLookup.Prefix);
        }

        [Fact]
        public void IdKey_IsConstant()
        {
            Assert.True(typeof(MvcLookup).GetField("IdKey").IsLiteral);
            Assert.Equal("LookupIdKey", MvcLookup.IdKey);
        }

        [Fact]
        public void AcKey_IsConstant()
        {
            Assert.True(typeof(MvcLookup).GetField("AcKey").IsLiteral);
            Assert.Equal("LookupAcKey", MvcLookup.AcKey);
        }

        #endregion

        #region MvcLookup()

        [Fact]
        public void MvcLookup_Defaults()
        {
            MvcLookup actual = Substitute.For<MvcLookup>();

            Assert.Equal("MvcLookupDialog", actual.Dialog);
            Assert.Empty(actual.AdditionalFilters);
            Assert.Equal(20, actual.Filter.Rows);
            Assert.Empty(actual.Columns);
        }

        #endregion
    }
}
