﻿using System;
using System.Linq;
using Xunit;

namespace NonFactors.Mvc.Lookup.Tests.Unit
{
    public class LookupQueryTests
    {
        [Fact]
        public void IsOrdered_False()
        {
            Assert.False(LookupQuery.IsOrdered(new Object[0].OrderBy(model => 0).AsQueryable()));
        }

        [Fact]
        public void IsOrdered_True()
        {
            Assert.True(LookupQuery.IsOrdered(new Object[0].AsQueryable().OrderBy(model => 0)));
        }
    }
}
